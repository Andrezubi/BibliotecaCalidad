using System.Globalization;
using System.Text;
using Backend.Application.DTOs;

namespace Backend.Infrastructure.Pdf;

// PDF A4 autocontenido, sin dependencias externas. Todos los datos provienen
// de FinePayment en base de datos; nunca se genera un recibo desde el preview.
public static class FineReceiptPdfGenerator
{
    public static byte[] Generate(FinePaymentReceiptDto receipt)
    {
        var content = new StringBuilder();
        static string Esc(string text)
        {
            // Los strings PDF utilizan WinAnsi; los caracteres no representables
            // se sustituyen para no corromper el flujo PDF.
            var clean = new string((text ?? string.Empty)
                .Select(c => (c >= 32 && c <= 126) || (c >= 160 && c <= 255) ? c : '?').ToArray());
            return clean.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
        }
        void Text(int x, int y, string value, int size = 11, bool bold = false)
        {
            content.Append($"BT /{(bold ? "F2" : "F1")} {size} Tf 1 0 0 1 {x} {y} Tm ({Esc(value)}) Tj ET\n");
        }
        void Rule(int y) => content.Append($"0.78 0.81 0.86 RG 1 w 48 {y} m 547 {y} l S\n");
        void Row(ref int y, string label, string value)
        {
            Text(52, y, label, 10);
            var chunks = Wrap(value, 47).ToList();
            foreach (var chunk in chunks)
            {
                Text(225, y, chunk, 10, true);
                y -= 18;
            }
            y -= 9;
        }

        Text(52, 775, "BIBLIOTECA - COMPROBANTE DE PAGO", 17, true);
        Text(52, 752, "Recibo de multa por devolución tardía", 11);
        Rule(735);
        var cursor = 708;
        Row(ref cursor, "Número de recibo", receipt.ReceiptNumber);
        Row(ref cursor, "Fecha y hora de pago", receipt.PaidAt.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture));
        Row(ref cursor, "Lector", receipt.BorrowerName);
        Row(ref cursor, "Libro", receipt.BookTitle);
        Row(ref cursor, "Motivo", "Devolución fuera de plazo");
        Row(ref cursor, "Días hábiles de retraso", receipt.OverdueDays.ToString(CultureInfo.InvariantCulture));
        Row(ref cursor, "Tarifa diaria", "Bs " + receipt.DailyRate.ToString("0.00", CultureInfo.InvariantCulture));
        Row(ref cursor, "Cobrado por", receipt.LibrarianName);
        Rule(cursor - 3);
        Text(52, cursor - 38, "TOTAL PAGADO", 14, true);
        Text(365, cursor - 38, "Bs " + receipt.Amount.ToString("0.00", CultureInfo.InvariantCulture), 16, true);
        Text(52, cursor - 78, "ESTADO: PAGADO", 11, true);
        Rule(110);
        Text(52, 91, "Comprobante emitido desde un pago registrado en el sistema.", 9);
        Text(52, 75, "Puede volver a descargarse desde el detalle del préstamo.", 9);

        var streamBytes = Encoding.Latin1.GetBytes(content.ToString());
        using var pdf = new MemoryStream();
        void Write(string value)
        {
            var bytes = Encoding.ASCII.GetBytes(value);
            pdf.Write(bytes, 0, bytes.Length);
        }

        Write("%PDF-1.4\n");
        var offsets = new long[7];
        void Obj(int n, string body)
        {
            offsets[n] = pdf.Position;
            Write($"{n} 0 obj\n{body}\nendobj\n");
        }
        Obj(1, "<< /Type /Catalog /Pages 2 0 R >>");
        Obj(2, "<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
        Obj(3, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R /F2 5 0 R >> >> /Contents 6 0 R >>");
        Obj(4, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>");
        Obj(5, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>");
        offsets[6] = pdf.Position;
        Write($"6 0 obj\n<< /Length {streamBytes.Length} >>\nstream\n");
        pdf.Write(streamBytes, 0, streamBytes.Length);
        Write("\nendstream\nendobj\n");
        var xrefOffset = pdf.Position;
        Write("xref\n0 7\n0000000000 65535 f \n");
        for (var i = 1; i <= 6; i++) Write($"{offsets[i]:0000000000} 00000 n \n");
        Write($"trailer\n<< /Size 7 /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF\n");
        return pdf.ToArray();
    }

    private static IEnumerable<string> Wrap(string text, int maxLength)
    {
        var words = (text ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var line = new StringBuilder();
        foreach (var word in words)
        {
            // Evita que una palabra excesivamente larga salga de la página.
            if (word.Length > maxLength)
            {
                if (line.Length > 0) { yield return line.ToString(); line.Clear(); }
                for (var i = 0; i < word.Length; i += maxLength)
                    yield return word.Substring(i, Math.Min(maxLength, word.Length - i));
                continue;
            }
            if (line.Length + word.Length + (line.Length == 0 ? 0 : 1) > maxLength)
            {
                yield return line.ToString();
                line.Clear();
            }
            if (line.Length > 0) line.Append(' ');
            line.Append(word);
        }
        if (line.Length > 0) yield return line.ToString();
        if (words.Length == 0) yield return "-";
    }
}

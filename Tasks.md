# TASKS.md — Revisión integral de préstamos, devoluciones y multas

## 1. Objetivo principal

Analizar, corregir e integrar profesionalmente todo el flujo de préstamos de libros de Biblioteca Virtual, desde la creación del préstamo hasta la devolución de cada ejemplar y la generación de multas por retraso.

El objetivo no es únicamente cambiar la apariencia de las pantallas. Se debe comprobar que **el frontend, el backend y la base de datos trabajen de manera coherente**, que los estados se actualicen correctamente y que las multas se calculen y registren cuando corresponda.

El agente debe revisar el código existente antes de implementar cualquier cambio. Debe reutilizar la arquitectura, los servicios, los repositorios, los DTOs, las entidades, los estilos y los componentes existentes siempre que sea posible.

No reconstruir funcionalidades desde cero si ya existen implementaciones que pueden corregirse o ampliarse.

---

## 2. Fase 1 — Inspección completa del proyecto

### 2.1. Backend

- [ ] Revisar la estructura real de carpetas y archivos.
- [ ] Leer los archivos `Program.cs` y la configuración de inyección de dependencias.
- [ ] Revisar la configuración de la conexión a la base de datos sin exponer credenciales.
- [ ] Identificar todos los controladores, servicios, repositorios, interfaces, DTOs, entidades y validadores relacionados con préstamos, devoluciones, ejemplares y multas.
- [ ] Revisar cómo se crean los préstamos y cómo se asocian los ejemplares prestados.
- [ ] Revisar cómo se calculan y actualizan los estados de préstamos y ejemplares.
- [ ] Revisar cómo se registra una devolución y qué campos se actualizan.
- [ ] Revisar si existe un servicio de multas y cómo se calcula el importe.
- [ ] Comprobar si la generación de multas está implementada en el backend o si actualmente depende del frontend.
- [ ] Revisar las validaciones, las transacciones y el manejo de errores.
- [ ] Comprobar la autenticación y los permisos necesarios para crear préstamos, devolver ejemplares y consultar multas.

### 2.2. Frontend

- [ ] Revisar la estructura real de carpetas y archivos del frontend.
- [ ] Identificar la pantalla que muestra únicamente los libros prestados y contiene el botón actual de devolución.
- [ ] Identificar la pantalla del listado general de préstamos.
- [ ] Identificar la pantalla de detalle que se abre mediante el botón del ojo.
- [ ] Revisar los archivos `.cshtml` y `.cshtml.cs` de las páginas involucradas.
- [ ] Revisar los servicios HTTP que consumen la API y los DTOs utilizados.
- [ ] Comprobar cómo se muestran los estados actuales y cómo se actualizan después de una operación.
- [ ] Revisar las redirecciones, las notificaciones, las alertas y los mensajes de éxito y error.
- [ ] Comprobar si la pantalla muestra información desactualizada después de devolver un ejemplar.
- [ ] Revisar el comportamiento cuando una solicitud falla o la API no responde.

### 2.3. Estilos y estructura visual

Antes de modificar las pantallas, leer y analizar:

- [ ] `wwwroot/css/site.css`.
- [ ] `_Layout.cshtml` y cualquier layout adicional utilizado por las páginas afectadas.
- [ ] Las vistas parciales y los componentes compartidos.
- [ ] Los archivos JavaScript relacionados con formularios, botones, alertas y modales.
- [ ] Los estilos y componentes de Bootstrap y Bootstrap Icons utilizados en el proyecto.
- [ ] Las convenciones visuales de las tablas, los botones, las etiquetas de estado, las tarjetas y los formularios.

Toda modificación visual debe mantener la identidad del sistema. Reutilizar los colores, las tipografías, los espacios, los bordes, las sombras y los componentes existentes.

No introducir un diseño diferente para la pantalla de multas ni modificar estilos globales sin revisar sus efectos en las demás páginas.

### 2.4. Base de datos

Revisar el `LibraryDbContext`, las entidades reales, las relaciones, las claves y las restricciones de la base de datos.

Prestar especial atención a:

- `Loan`: información general del préstamo, fechas y estado.
- `LoanItem`: relación entre el préstamo y cada ejemplar, estado individual y fecha de devolución.
- `Copy`: ejemplar físico y estado de disponibilidad.
- `Book`: información del libro.
- `Fine`: multa asociada al préstamo y al ejemplar.
- `FinePayment`: pagos de multas, si participan en el flujo actual.
- `User`: lector y usuario responsable de registrar las operaciones.

Utilizar los nombres y las propiedades que realmente existan en los modelos del proyecto. Los nombres anteriores sirven como guía de inspección, no como autorización para inventar propiedades.

Revisar también las restricciones de unicidad, las claves compuestas y las relaciones entre `Loan`, `LoanItem`, `Copy` y `Fine`.

### Resultado de esta fase

Antes de implementar:

1. Identificar los archivos responsables de cada parte del flujo.
2. Explicar cómo funciona actualmente la creación de préstamos.
3. Explicar cómo se actualizan los estados de los préstamos y ejemplares.
4. Explicar cómo se realiza actualmente la devolución.
5. Explicar cómo se calculan y guardan las multas.
6. Identificar los errores encontrados y los archivos que será necesario modificar.
7. Identificar las reglas de negocio que no estén definidas.

Después de este análisis, proceder con la implementación sin realizar refactorizaciones ajenas a la tarea.

---

## 3. Fase 2 — Revisar el flujo completo de préstamos

Revisar el proceso de principio a fin, no solamente la pantalla de devolución.

### 3.1. Creación del préstamo

- [ ] Verificar que el préstamo se registre correctamente.
- [ ] Verificar que cada ejemplar prestado quede asociado al préstamo correspondiente.
- [ ] Comprobar que los estados del préstamo, de sus elementos y de los ejemplares sean coherentes.
- [ ] Comprobar que se registre correctamente la fecha del préstamo y la fecha límite de devolución.
- [ ] Validar que no se preste un ejemplar que no esté disponible según las reglas del sistema.
- [ ] Verificar que los errores de persistencia no dejen préstamos incompletos.
- [ ] Comprobar que los listados y detalles reflejen la información realmente almacenada.

### 3.2. Seguimiento del préstamo

- [ ] Revisar cómo se muestran los préstamos activos.
- [ ] Revisar cómo se determina si un préstamo o ejemplar está vencido.
- [ ] Comprobar si el estado vencido se calcula al consultar los datos o si necesita una actualización persistente.
- [ ] Identificar si existen filtros por estado que no reflejan correctamente la información.
- [ ] Evitar que un ejemplar ya devuelto aparezca como pendiente de devolución.
- [ ] Evitar que los cambios de estado de un ejemplar afecten incorrectamente a los demás ejemplares del préstamo.

### 3.3. Coherencia entre estados

Analizar los estados reales del sistema y documentar cómo se relacionan.

La implementación debe distinguir entre:

- Estado general del préstamo.
- Estado individual del elemento del préstamo.
- Estado físico o de disponibilidad del ejemplar.
- Estado de la multa.

No asumir que estos estados son intercambiables.

Si la base de datos distingue estados como `Active`, `Overdue`, `Completed`, `Returned` o `Available`, comprobar cuáles corresponden realmente a cada entidad y respetar sus valores permitidos.

**Regla importante:** devolver un ejemplar no debe marcar automáticamente todos los demás ejemplares del préstamo como devueltos. El estado general del préstamo debe actualizarse según el estado de todos sus elementos y las reglas de negocio existentes.

---

## 4. Fase 3 — Modificar el flujo y las pantallas de devolución

### 4.1. Primera pantalla: libros prestados

Actualmente, la pantalla que muestra únicamente los libros prestados tiene un botón de devolución. Esta no debe seguir siendo el lugar desde el que se realiza la devolución.

- [ ] Identificar y retirar el botón de devolución de esta pantalla.
- [ ] Mantener las demás funcionalidades de la página.
- [ ] Comprobar si existen otros procesos que dependan del botón actual antes de eliminar su implementación.
- [ ] No borrar indiscriminadamente los servicios o endpoints que puedan seguir siendo necesarios.

### 4.2. Segunda pantalla: listado de préstamos

Esta pantalla muestra los préstamos registrados y permite abrir el detalle mediante el botón del ojo.

- [ ] Mantener el listado general de préstamos.
- [ ] Conservar el botón del ojo para acceder al detalle.
- [ ] Comprobar que se abra el préstamo seleccionado y no otro.
- [ ] Mantener los filtros, las acciones y los estilos existentes.
- [ ] Comprobar que el estado que se muestra en el listado coincida con el estado real del préstamo.

### 4.3. Detalle del préstamo

El detalle del préstamo debe convertirse en el punto principal para realizar devoluciones.

- [ ] Mostrar la información del préstamo y los ejemplares que contiene.
- [ ] Mostrar el libro y el ejemplar correspondiente a cada elemento.
- [ ] Mostrar la fecha del préstamo y su fecha límite de devolución.
- [ ] Mostrar el estado individual de cada ejemplar.
- [ ] Mostrar la fecha de devolución cuando corresponda.
- [ ] Añadir un botón **Devolver** al lado de cada ejemplar que todavía pueda devolverse.
- [ ] No mostrar el botón de devolución para ejemplares ya devueltos.
- [ ] No mostrar el botón para elementos que no admitan devolución según las reglas del sistema.
- [ ] Mantener la coherencia visual con el resto de las pantallas.

### 4.4. Comportamiento después de devolver

Después de confirmar una devolución, la pantalla debe reflejar inmediatamente el resultado real de la operación.

- [ ] Actualizar o recargar los datos del detalle del préstamo.
- [ ] Cambiar el estado del ejemplar devuelto.
- [ ] Hacer que el botón **Devolver** desaparezca para ese ejemplar.
- [ ] Mostrar una etiqueta o estado de devolución utilizando los componentes existentes.
- [ ] Actualizar el estado general del préstamo si corresponde.
- [ ] Actualizar los datos de la lista de préstamos cuando el usuario regrese a ella.
- [ ] Evitar que el usuario tenga que cerrar sesión, recargar manualmente toda la aplicación o navegar por varias páginas para ver los cambios.

El botón debe desaparecer como consecuencia de que el backend haya confirmado la devolución y el frontend haya recibido el estado actualizado. No debe desaparecer definitivamente si la operación falla.

---

## 5. Fase 4 — Gestión de estados activos y vencidos

Revisar el funcionamiento de los estados `Active` y `Overdue` o sus equivalentes reales.

### Comportamiento requerido

- [ ] Un ejemplar prestado y pendiente de devolución debe aparecer como activo cuando todavía está dentro del plazo.
- [ ] Cuando su fecha límite haya pasado y continúe pendiente de devolución, debe identificarse como vencido según las reglas del sistema.
- [ ] El estado vencido debe reflejarse correctamente en el detalle y en los listados relevantes.
- [ ] Un ejemplar devuelto no debe volver a aparecer como activo o vencido por el simple hecho de que su fecha límite haya pasado.
- [ ] El estado de los demás ejemplares del préstamo no debe modificarse incorrectamente.

### Revisar cuándo se actualiza el estado

Determinar si el sistema actualiza los estados:

- Al consultar los préstamos.
- Al abrir el detalle.
- Mediante una operación de actualización.
- Mediante un proceso programado, si existe.

Elegir la solución compatible con la arquitectura existente. No introducir procesos en segundo plano innecesarios ni modificar el esquema sin justificación.

La fecha límite debe evaluarse en el backend utilizando una referencia temporal coherente.

Si el sistema necesita persistir el estado `Overdue`, garantizar que la actualización sea consistente con el resto de la lógica y que no sobrescriba el estado de ejemplares ya devueltos.

---

## 6. Fase 5 — Implementar correctamente la devolución individual

- [ ] Identificar el préstamo y el ejemplar mediante sus identificadores reales.
- [ ] Si `LoanItem` utiliza una clave compuesta, utilizar los identificadores necesarios, como `LoanId` y `CopyId`.
- [ ] Verificar que el ejemplar pertenezca al préstamo seleccionado.
- [ ] Confirmar que el ejemplar todavía no se haya devuelto.
- [ ] Registrar la fecha y hora efectiva de devolución mediante el backend.
- [ ] Actualizar el estado individual del elemento del préstamo.
- [ ] Actualizar el estado de `Copy` según las reglas actuales.
- [ ] Actualizar el estado general de `Loan` cuando corresponda.
- [ ] Mantener la información de auditoría según las convenciones del proyecto.
- [ ] Impedir las devoluciones duplicadas.
- [ ] Manejar solicitudes simultáneas y errores de persistencia.
- [ ] Proteger los endpoints con la autorización correspondiente.

La operación debe modificar exclusivamente el ejemplar seleccionado, salvo los cambios adicionales que sean necesarios y estén justificados por las reglas de negocio.

---

## 7. Fase 6 — Activar las multas cuando corresponda

El sistema de multas debe integrarse con el proceso de devolución.

### 7.1. Devolución dentro del plazo

Si el ejemplar se devuelve a tiempo:

- [ ] Registrar la devolución.
- [ ] Actualizar los estados correspondientes.
- [ ] No crear ningún registro en `Fine`.
- [ ] No mostrar la pantalla de multa.
- [ ] Mostrar una confirmación normal de devolución exitosa.

### 7.2. Devolución después del vencimiento

Si el ejemplar se devuelve después de su fecha límite:

- [ ] Detectar el retraso en el backend.
- [ ] Calcular los días de retraso conforme a las reglas confirmadas del sistema.
- [ ] Calcular el importe de la multa.
- [ ] Registrar la multa en la tabla `Fine`.
- [ ] Relacionar la multa con el préstamo y el ejemplar correctos.
- [ ] Completar la devolución correctamente.
- [ ] Mostrar la pantalla con la información de la multa generada.
- [ ] Actualizar el estado del préstamo y del ejemplar según corresponda.

**La generación de multas no debe depender únicamente de que el frontend detecte un estado vencido.** El backend debe verificar la fecha límite y la fecha efectiva de devolución antes de decidir si corresponde crear la multa.

Si el ejemplar ya estaba vencido, pero se devuelve posteriormente, la multa debe calcularse a partir del retraso real según la regla de negocio. El estado `Overdue` sirve para identificar la situación, pero no sustituye la comprobación de las fechas.

---

## 8. Fase 7 — Revisar el cálculo de multas

Antes de modificar la fórmula, investigar cómo funciona actualmente el sistema.

- [ ] Buscar una tarifa diaria configurada.
- [ ] Buscar servicios, métodos o validadores relacionados con multas.
- [ ] Revisar si existen reglas de días de gracia, redondeo o excepciones.
- [ ] Verificar el tipo de datos usado para almacenar importes.
- [ ] Confirmar si la tarifa puede variar según alguna regla existente.
- [ ] Reutilizar la lógica existente cuando sea correcta.

Si no existe una fórmula específica y se confirma que la tarifa es fija por día, puede utilizarse:

`Importe = Días de retraso × Tarifa diaria`

No inventar la tarifa, los días de gracia ni una política de redondeo. Si falta una regla económica imprescindible, documentar qué información falta y solicitar confirmación antes de establecerla.

El cálculo definitivo debe ejecutarse en el backend. El frontend no debe enviar un importe que el backend acepte sin verificarlo.

---

## 9. Fase 8 — Persistencia de la multa en `Fine`

Revisar el modelo real de `Fine` y su relación con los elementos del préstamo.

Si el esquema actual contiene campos equivalentes a los siguientes, comprobar su uso:

- `LoanId`
- `CopyId`
- `DailyRate`
- `OverdueDays`
- `Amount`
- `Status`
- Campos de auditoría correspondientes

Implementar lo siguiente:

- [ ] Crear una multa cuando la devolución atrasada lo requiera.
- [ ] Guardar el préstamo y el ejemplar correctos.
- [ ] Guardar la tarifa, los días de retraso y el importe calculado.
- [ ] Asignar un estado inicial válido para el modelo real.
- [ ] Respetar las claves foráneas y las restricciones de la base de datos.
- [ ] Evitar multas duplicadas.
- [ ] Comprobar las restricciones de unicidad existentes, especialmente si solo se permite una multa por `LoanId` y `CopyId`.
- [ ] Manejar correctamente los reintentos de una solicitud.
- [ ] Evitar estados parciales cuando la devolución y la creación de la multa formen parte de una misma operación.
- [ ] Usar transacciones cuando corresponda para mantener la consistencia.

Si ya existe una multa para el ejemplar, revisar las reglas actuales antes de intentar crear otra. No desactivar las restricciones de unicidad para solucionar un error de lógica.

No ejecutar `DROP DATABASE`, `DROP TABLE`, `TRUNCATE` ni recrear la base de datos.

---

## 10. Fase 9 — Pantalla de multa generada

Cuando una devolución atrasada genere una multa correctamente, mostrar una pantalla profesional que presente la información de la operación.

### Información necesaria

- [ ] Confirmación de la devolución.
- [ ] Libro y ejemplar devueltos.
- [ ] Identificador del préstamo.
- [ ] Fecha límite de devolución.
- [ ] Fecha efectiva de devolución.
- [ ] Días de retraso.
- [ ] Tarifa diaria aplicada.
- [ ] Importe total de la multa.
- [ ] Estado actual de la multa.

Mostrar solamente la información que realmente esté disponible y sea necesaria.

### Diseño y navegación

- [ ] Reutilizar el layout y los estilos existentes.
- [ ] Mostrar claramente el importe y los días de retraso.
- [ ] Utilizar los mismos botones, alertas, colores e iconos que el resto de la aplicación.
- [ ] Permitir volver al detalle del préstamo o al listado correspondiente.
- [ ] Mostrar mensajes claros si no se pudo completar la operación.
- [ ] No generar la multa desde la página de presentación; esta debe mostrar la información registrada por el backend.

Si el sistema ya dispone de una pantalla de detalle de multas, revisar si puede reutilizarse antes de crear otra.

---

## 11. Fase 10 — Integración entre frontend, backend y base de datos

- [ ] Reutilizar los endpoints existentes cuando sean adecuados.
- [ ] Adaptar los controladores, servicios, repositorios y DTOs necesarios.
- [ ] Mantener las responsabilidades de cada capa.
- [ ] Evitar reglas de negocio duplicadas entre frontend y backend.
- [ ] Mantener las convenciones de rutas, nombres y respuestas de la API.
- [ ] Gestionar errores de validación, errores HTTP y errores de persistencia.
- [ ] Actualizar el detalle y el listado después de las operaciones exitosas.
- [ ] Verificar que las pantallas utilicen los estados devueltos por el backend.
- [ ] Evitar estados ficticios que cambien solamente en la interfaz.
- [ ] Comprobar la autorización en el backend, no solamente la visibilidad de los botones.
- [ ] Revisar el efecto de los cambios sobre otras pantallas que reutilicen los mismos servicios o DTOs.

---

## 12. Fase 11 — Pruebas obligatorias

### Caso A: préstamo normal

- [ ] Crear un préstamo.
- [ ] Comprobar que sus ejemplares se registren correctamente.
- [ ] Comprobar las fechas y los estados.
- [ ] Verificar que el préstamo aparezca correctamente en el listado y en el detalle.

### Caso B: ejemplar activo dentro del plazo

- [ ] Abrir el detalle.
- [ ] Comprobar que el ejemplar esté activo.
- [ ] Devolverlo.
- [ ] Verificar que cambie el estado.
- [ ] Verificar que desaparezca el botón de devolución.
- [ ] Verificar que no se genere una multa.

### Caso C: ejemplar activo cuya fecha límite ya pasó

- [ ] Verificar que el ejemplar se identifique como vencido según las reglas del sistema.
- [ ] Intentar devolverlo.
- [ ] Verificar que el backend calcule el retraso.
- [ ] Verificar que la multa se registre correctamente.
- [ ] Verificar que se muestre la pantalla de multa.
- [ ] Verificar que el ejemplar ya no aparezca como pendiente de devolución.

### Caso D: préstamo con varios ejemplares

- [ ] Crear o utilizar un préstamo con varios ejemplares.
- [ ] Devolver solo uno.
- [ ] Verificar que los demás ejemplares mantengan sus estados.
- [ ] Comprobar que el préstamo general conserve el estado correcto.
- [ ] Devolver los ejemplares restantes y verificar el resultado final.

### Caso E: devolución duplicada

- [ ] Intentar devolver un ejemplar que ya fue devuelto.
- [ ] Verificar que la operación sea rechazada.
- [ ] Comprobar que no se cree otra multa.
- [ ] Comprobar que no se alteren las fechas o los estados existentes.

### Caso F: errores de operación

- [ ] Probar identificadores inexistentes.
- [ ] Intentar devolver un ejemplar que no pertenece al préstamo.
- [ ] Probar errores de API y de base de datos.
- [ ] Comprobar que no queden registros parcialmente actualizados.
- [ ] Verificar los permisos de los usuarios.

### Caso G: interfaz

- [ ] Comprobar que ya no exista el botón de devolución en la pantalla original de libros prestados.
- [ ] Comprobar que cada ejemplar elegible tenga su botón en el detalle del préstamo.
- [ ] Comprobar que el botón desaparezca tras una devolución confirmada.
- [ ] Comprobar que las etiquetas de estado se actualicen.
- [ ] Comprobar que la pantalla de multa respete los estilos existentes.
- [ ] Verificar que el diseño responsive y la navegación sigan funcionando.

---

## 13. Fase 12 — Validación final

- [ ] Compilar el backend.
- [ ] Compilar el frontend.
- [ ] Ejecutar las pruebas automatizadas disponibles.
- [ ] Añadir o actualizar pruebas cuando la estructura actual lo permita.
- [ ] Revisar errores y advertencias relevantes.
- [ ] Verificar las modificaciones de la base de datos.
- [ ] Confirmar que los estados de los préstamos y ejemplares sean coherentes.
- [ ] Confirmar que las multas se calculen y se guarden correctamente.
- [ ] Confirmar que no se hayan introducido cambios ajenos a la tarea.
- [ ] Revisar los archivos modificados antes de finalizar.

No afirmar que las compilaciones o las pruebas pasaron si no se ejecutaron realmente.

---

## 14. Restricciones obligatorias

1. No modificar la arquitectura completa del proyecto.
2. No crear una segunda implementación de servicios o reglas que ya existan.
3. No eliminar archivos sin revisar sus dependencias.
4. No inventar campos, relaciones, estados o tarifas.
5. No realizar cambios destructivos en la base de datos.
6. No permitir devoluciones ni multas duplicadas.
7. No realizar el cálculo definitivo de multas únicamente en el frontend.
8. No marcar todo un préstamo como devuelto al devolver un solo ejemplar.
9. No mantener visible el botón de devolución para ejemplares ya devueltos.
10. No actualizar visualmente una devolución como exitosa si el backend la rechazó.
11. No introducir estilos que se aparten de los utilizados por el proyecto.
12. No modificar funcionalidades ajenas al flujo de préstamos, devoluciones y multas.
13. Si se encuentra una contradicción entre las instrucciones, el código y el esquema real, documentarla y resolverla de manera segura antes de introducir cambios incompatibles.

---

## 15. Criterios de aceptación

La tarea estará terminada cuando:

- [ ] El proceso de creación de préstamos funcione correctamente.
- [ ] El listado y el detalle reflejen los estados reales.
- [ ] La pantalla original de libros prestados ya no tenga el botón de devolución.
- [ ] La devolución se realice desde el detalle del préstamo.
- [ ] Cada ejemplar tenga su propia acción de devolución cuando corresponda.
- [ ] La devolución actualice el estado de forma persistente.
- [ ] El botón desaparezca tras confirmarse la devolución.
- [ ] Los ejemplares cuyo plazo haya pasado se identifiquen correctamente como vencidos.
- [ ] Una devolución dentro del plazo no genere multa.
- [ ] Una devolución fuera del plazo genere la multa que corresponda.
- [ ] La multa se registre correctamente en `Fine`.
- [ ] La pantalla de multa muestre la información correcta.
- [ ] No existan devoluciones ni multas duplicadas.
- [ ] Los estados de `Loan`, `LoanItem` y `Copy` sean coherentes.
- [ ] Las pantallas respeten los estilos, layouts y componentes existentes.
- [ ] Backend y frontend compilen y los resultados de las pruebas estén documentados.

---

## 16. Informe final del agente

Al terminar, presentar un informe con:

1. Resumen del funcionamiento anterior y de los problemas detectados.
2. Archivos del frontend y backend inspeccionados.
3. Archivos modificados y justificación de cada cambio.
4. Explicación del flujo de préstamo y devolución implementado.
5. Explicación de cómo se actualizan los estados `Active`, `Overdue`, `Returned` y sus equivalentes reales.
6. Explicación de cómo se calcula y persiste una multa.
7. Regla y tarifa utilizadas, indicando dónde estaban definidas.
8. Explicación de cómo se evita generar multas duplicadas.
9. Estilos y componentes reutilizados.
10. Compilaciones y pruebas realmente ejecutadas.
11. Errores pendientes y decisiones que requieran confirmación.

No marcar una tarea como completada si no ha sido implementada y verificada.

---

## Orden obligatorio de ejecución

1. Analizar el frontend y el backend completos en las áreas relacionadas.
2. Leer las páginas, los layouts, las hojas CSS y los componentes compartidos.
3. Revisar las entidades, las relaciones y las restricciones reales de la base de datos.
4. Analizar el flujo existente de préstamo, vencimiento, devolución y multas.
5. Identificar los errores y definir los archivos que deben modificarse.
6. Corregir el flujo de estados.
7. Implementar la devolución individual desde el detalle.
8. Integrar el cálculo y la persistencia de multas.
9. Adaptar las pantallas respetando el diseño actual.
10. Ejecutar las pruebas y validar la consistencia de los datos.
11. Presentar el informe final.

**Prioridad máxima:** conseguir que prestar, consultar, vencer, devolver y generar multas funcionen como un único flujo coherente y confiable, sin romper las funcionalidades existentes ni alterar innecesariamente la arquitectura y el diseño de Biblioteca Virtual.
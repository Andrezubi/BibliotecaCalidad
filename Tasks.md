# TASK: Corregir el sistema de multas, vencimiento de préstamos y devolución de libros

## Objetivo principal

Revisa nuevamente todo el funcionamiento del sistema de préstamos, vencimientos, devoluciones y multas de la Biblioteca Virtual. Actualmente, las multas no están funcionando correctamente y el proceso de devolución presenta errores importantes.

Necesito que investigues el código existente, identifiques las causas reales y soluciones el problema tanto en el **backend como en el frontend**, respetando la arquitectura, las entidades, la base de datos y la lógica actual del proyecto.

**No hagas cambios superficiales ni te limites a modificar el texto que aparece en pantalla.** Los estados deben actualizarse correctamente en la base de datos y el cálculo de las multas debe funcionar de principio a fin.

---

## 1. Revisar el funcionamiento actual de las multas

Inspecciona todo el código relacionado con:

- Préstamos y sus detalles.
- Ítems o elementos individuales de cada préstamo.
- Devoluciones de libros.
- Estados de los préstamos y de cada ítem.
- Fechas de préstamo, vencimiento y devolución.
- Cálculo de días de retraso.
- Cálculo y registro del importe de las multas.
- Servicios, controladores, DTOs, modelos y repositorios relacionados.
- Páginas Razor, JavaScript, formularios, eventos y modales.
- Consultas y relaciones de la base de datos utilizadas en este proceso.

Busca especialmente errores en los que:

- La multa no se calcula cuando un libro está vencido.
- El sistema no detecta que se ha superado la fecha límite de devolución.
- Se devuelve un libro sin comprobar si tiene una multa pendiente.
- El frontend no recibe o no utiliza la información de la multa devuelta por el backend.
- El modal no se abre al pulsar el botón de devolución.
- El estado del ítem cambia incorrectamente a `Completado`.
- El estado del préstamo o de su detalle se actualiza sin considerar los libros vencidos.
- El sistema calcula la multa, pero no la guarda o no la muestra.
- El sistema pierde el estado de vencido después de registrar una devolución.

Determina la causa exacta de cada problema antes de implementar la solución.

## 2. Corregir el cambio automático de estado a vencido

Cuando un libro prestado tenga una fecha límite de devolución y esta fecha haya pasado sin que se haya registrado su devolución, el sistema debe reconocerlo como vencido.

Implementa o corrige la lógica para que:

1. Se identifiquen los préstamos o ítems cuya fecha límite ya pasó.
2. Se compruebe que el libro todavía no ha sido devuelto.
3. Se actualice el estado correspondiente a `Vencido`.
4. El cambio se persista en la base de datos.
5. El estado se refleje correctamente en la lista de préstamos, en la sección de ítems y en el detalle del préstamo.
6. Se calcule la multa según los días de retraso y las reglas de negocio que ya existan en el proyecto.

No cambies a `Vencido` un libro que ya fue devuelto correctamente.

**Importante:** revisa si la actualización de vencimientos depende de que un usuario abra una página determinada. Si es así, evalúa la implementación existente y corrige el mecanismo para que el estado no dependa únicamente de lo que se muestra en pantalla. Utiliza la solución compatible con la arquitectura actual del proyecto.

## 3. Corregir el botón «Devolver libro» en la sección de ítems

Actualmente, al pulsar el botón «Devolver libro», el sistema devuelve el libro directamente, cambia el estado a `Completado` tanto en los ítems como en el detalle y no muestra ningún modal con la información de la multa.

Este comportamiento debe corregirse.

El flujo esperado es el siguiente:

1. El usuario pulsa «Devolver libro».
2. El sistema identifica el ítem específico que se quiere devolver.
3. Se comprueba la fecha límite de devolución y la fecha actual.
4. Se determina si existe retraso.
5. Se calcula o recupera el importe de la multa aplicable.
6. Se obtiene toda la información necesaria para mostrar el detalle de la multa.
7. Se abre un modal antes de confirmar definitivamente la devolución, cuando corresponda mostrar la multa.
8. El modal muestra la información correcta del libro y de la multa.
9. El usuario puede confirmar la devolución después de revisar el detalle.
10. El sistema registra la devolución y actualiza los estados de forma coherente.

No permitas que el botón ejecute directamente la devolución definitiva antes de completar el flujo requerido para mostrar la multa.

Si el libro no está vencido y no corresponde aplicar una multa, conserva el flujo de devolución normal o presenta una confirmación apropiada, según el diseño actual del proyecto.

## 4. Implementar correctamente el modal de detalle de multa

El modal debe aparecer al intentar devolver un libro vencido con una multa aplicable.

Debe mostrar, como mínimo, la siguiente información:

- Título del libro.
- Fecha en que se realizó el préstamo.
- Fecha límite de devolución.
- Fecha actual o fecha efectiva de devolución, según corresponda.
- Cantidad de días de retraso.
- Importe de la multa.
- Estado de la multa, si existe en el modelo actual.
- Total de la multa que corresponde al libro.

Si el sistema ya tiene una estructura visual para mostrar multas, reutilízala y mejórala en lugar de crear componentes duplicados.

### Investiga por qué el modal no aparece

Verifica específicamente:

- Si el botón de devolución está conectado al evento correcto.
- Si el formulario ejecuta un envío tradicional que recarga la página antes de abrir el modal.
- Si el modal existe en el HTML generado.
- Si los identificadores del modal y del botón coinciden.
- Si Bootstrap y sus dependencias están cargados correctamente.
- Si JavaScript recibe la respuesta correcta del backend.
- Si hay errores en la consola del navegador.
- Si el backend responde con una redirección o un resultado incompatible con el código JavaScript.
- Si los datos de la multa llegan correctamente al frontend.
- Si se está intentando abrir el modal antes de que el contenido esté disponible.
- Si el código registra la devolución antes de mostrar el modal.

Corrige la causa real. No soluciones el problema únicamente agregando una llamada para abrir el modal sin garantizar que los datos sean correctos.

## 5. Corregir la lógica de estados

Revisa los estados de los préstamos, de los detalles y de los ítems para determinar cuál representa el estado general y cuál representa el estado individual de cada libro.

Actualmente, un libro vencido pasa incorrectamente a `Completado` después de pulsar «Devolver libro».

La lógica debe distinguir entre el estado previo a la devolución y el estado posterior a una devolución efectiva.

Reglas obligatorias:

- Un libro cuya fecha límite haya pasado y que todavía no se haya devuelto debe figurar como `Vencido`.
- Un libro que siga pendiente de devolución no debe marcarse como `Completado` solo porque se haya abierto el detalle o iniciado el proceso de devolución.
- Un libro devuelto correctamente debe reflejar su devolución efectiva mediante el estado final que corresponda al modelo existente.
- Si la devolución todavía no se ha confirmado, no debe registrarse como definitiva.
- El estado general del préstamo debe calcularse según sus ítems y las reglas actuales del sistema, no cambiarse indiscriminadamente.
- La devolución de un libro no debe modificar incorrectamente el estado de otros libros del mismo préstamo.
- Los estados que se muestran en las listas y en el detalle deben coincidir con los datos persistidos.

**Importante:** no fuerces el estado a `Vencido` después de una devolución ya confirmada si el modelo utiliza `Completado` para representar una devolución efectiva. En su lugar, conserva el registro del vencimiento y la multa, y actualiza el estado final de devolución según las reglas de negocio existentes.

Si el sistema necesita diferenciar el estado de devolución del estado de vencimiento o de la multa, analiza la estructura actual antes de modificarla. No agregues nuevos estados ni columnas sin comprobar primero si son necesarios y compatibles con la base de datos.

## 6. Revisar el cálculo y persistencia de las multas

Comprueba que la multa se calcule con los datos reales del préstamo y las reglas de negocio configuradas en el proyecto.

Verifica:

- Cómo se determina la fecha límite.
- Cómo se calculan los días de retraso.
- Cuál es la tarifa diaria, si el sistema utiliza una.
- Si existe un período de gracia.
- Si el cálculo incluye correctamente el día de devolución.
- Si se utiliza la fecha actual o una fecha efectiva de devolución.
- Si se evita generar multas duplicadas.
- Si la multa se guarda correctamente cuando corresponde.
- Si se recupera correctamente para mostrarla en el modal y en el detalle.
- Si el importe mostrado coincide con el importe registrado.

No inventes tarifas, reglas de negocio ni valores monetarios. Utiliza la configuración y las reglas existentes. Si falta información necesaria, identifica el problema y solicita aclaración en lugar de asumir un importe arbitrario.

## 7. Revisar backend y frontend de forma integral

### Backend

Revisa los servicios, controladores, repositorios, entidades y DTOs involucrados en el proceso.

Verifica que:

- La operación de devolución compruebe el estado y las fechas del ítem.
- El cálculo de la multa se ejecute en el momento correcto.
- La respuesta incluya los datos necesarios para el modal.
- La devolución y la persistencia de la multa sean coherentes.
- Se utilicen transacciones cuando sea necesario para evitar registros inconsistentes.
- Los errores se comuniquen correctamente al frontend.
- No se marquen préstamos completos cuando todavía existen ítems pendientes.
- Se respeten las relaciones y restricciones de la base de datos.

### Frontend

Revisa las páginas Razor, PageModels, servicios HTTP, JavaScript y componentes visuales implicados.

Verifica que:

- El botón envíe el identificador correcto del ítem.
- El frontend gestione correctamente la respuesta del backend.
- El modal reciba los datos reales de la multa.
- La devolución no se ejecute definitivamente antes de mostrar la información requerida.
- El usuario pueda confirmar o cancelar cuando corresponda.
- Después de confirmar, se actualicen las listas y el detalle sin mostrar estados antiguos.
- Los mensajes de error sean claros.
- No se dupliquen solicitudes ni devoluciones por eventos repetidos.

## 8. Respetar la arquitectura y el código existente

Antes de modificar archivos:

1. Analiza la estructura actual del proyecto.
2. Localiza todos los componentes relacionados con préstamos, ítems, devoluciones y multas.
3. Identifica cómo se comunica el frontend con el backend.
4. Comprueba el esquema real de la base de datos.
5. Sigue los patrones de arquitectura, nomenclatura y manejo de errores que ya utiliza el proyecto.

No reescribas módulos completos sin necesidad. No elimines funcionalidades existentes ni cambies contratos entre frontend y backend sin adaptar todas las dependencias afectadas.

No crees servicios, DTOs, tablas o columnas duplicadas si ya existe una implementación equivalente.

Si es necesario modificar la base de datos, explica el motivo y proporciona el cambio SQL o la migración correspondiente antes de darlo por resuelto.

## 9. Pruebas obligatorias

Prueba los siguientes escenarios de extremo a extremo:

### Caso A: libro devuelto dentro del plazo

- No debe existir retraso.
- No debe generarse una multa injustificada.
- La devolución debe completarse correctamente.
- No debe aparecer una multa inexistente.

### Caso B: libro vencido y todavía no devuelto

- El sistema debe identificarlo como `Vencido`.
- El estado debe persistirse en la base de datos.
- La multa debe calcularse de acuerdo con las reglas existentes.

### Caso C: devolver un libro vencido desde la sección de ítems

- Al pulsar «Devolver libro», debe aparecer el modal con el detalle de la multa.
- Los datos del modal deben corresponder al libro seleccionado.
- La devolución no debe ejecutarse definitivamente antes de completar la confirmación requerida.
- Al confirmar, debe registrarse la devolución y conservarse el historial del vencimiento y de la multa.
- La lista de ítems y el detalle del préstamo deben mostrar estados coherentes.

### Caso D: cancelar el modal

- La devolución no debe registrarse como completada.
- El ítem debe conservar su estado real.
- La multa no debe duplicarse ni perderse.
- Si el libro estaba vencido, debe seguir figurando como vencido mientras no se devuelva.

### Caso E: préstamo con varios libros

- Devolver un libro no debe modificar incorrectamente los demás ítems.
- Los ítems vencidos deben conservar su información correcta.
- El estado general del préstamo debe reflejar el estado real de todos sus ítems.

### Caso F: devolución repetida

- El sistema debe impedir que un mismo ítem se devuelva varias veces.
- No deben duplicarse multas ni registros de devolución.
- Debe mostrarse un mensaje apropiado si el libro ya fue devuelto.

### Caso G: actualización y persistencia

- Recarga la página después de realizar las operaciones.
- Comprueba que los estados, las fechas y las multas continúen siendo correctos.
- Verifica que el frontend no dependa únicamente de cambios visuales temporales.

## 10. Criterios de aceptación

La tarea se considera terminada únicamente cuando:

- El sistema identifica correctamente los libros vencidos.
- El estado de vencimiento se guarda y se muestra correctamente.
- El cálculo de multas funciona según las reglas existentes.
- El modal aparece cuando corresponde, antes de confirmar la devolución.
- El modal muestra información real y no valores ficticios.
- Cancelar no registra una devolución definitiva.
- Confirmar registra la devolución y conserva la información histórica de la multa.
- No se cambian incorrectamente los estados de otros ítems o del préstamo completo.
- La información permanece correcta después de recargar la página.
- No existen errores nuevos de compilación ni errores relevantes en la consola o en las solicitudes HTTP.
- Las pruebas de los escenarios anteriores se han ejecutado y sus resultados se han verificado.

## 11. Informe final obligatorio

Cuando termines, proporciona un informe con:

1. **Causa raíz:** por qué no funcionaban las multas y por qué no aparecía el modal.
2. **Archivos revisados y modificados:** indica la ruta exacta de cada archivo y qué cambiaste.
3. **Backend:** explica las correcciones realizadas en el cálculo, la persistencia y la devolución.
4. **Frontend:** explica cómo se solucionó la apertura del modal y la actualización de estados.
5. **Base de datos:** indica si fue necesario modificar el esquema o los datos.
6. **Pruebas realizadas:** especifica los escenarios probados y sus resultados reales.
7. **Pendientes:** menciona cualquier problema que no hayas podido verificar o resolver.

No afirmes que una funcionalidad está corregida si únicamente modificaste el código sin ejecutar las pruebas correspondientes.

**Prioridad máxima:** solucionar el flujo de devolución de un libro vencido, mostrar el modal con el detalle correcto de la multa y evitar que el sistema marque incorrectamente el préstamo como completado antes de registrar la devolución de forma coherente.
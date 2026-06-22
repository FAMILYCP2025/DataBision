# Native BI Finance — Manejo de Objeciones

**Guía para consultor DataBision · Uso en ventas y demos**

---

## Objeción 1: "Ya tengo Power BI"

**Respuesta corta:**
> "Power BI es la herramienta de visualización. Nosotros somos la capa de datos SAP que alimenta ese dashboard — o que lo reemplaza si no están usando Power BI activamente."

**Respuesta completa:**
> "Si ya tienen Power BI configurado y funcionando con datos de SAP B1 — perfecto, no cambiamos nada. Pero en la mayoría de casos que vemos, Power BI está instalado pero no tiene datos actualizados de SAP, o el connector no funciona bien, o nadie lo mantiene."
>
> "Native BI Finance resuelve exactamente eso: la extracción, clasificación y disponibilidad de datos contables desde SAP. Si quieren seguir usando Power BI para visualización, podemos exportar los datos al formato que necesiten. Si prefieren nuestro dashboard integrado, también está disponible."
>
> "¿Actualmente tienen Power BI conectado a SAP B1 con datos del libro diario?"

**Señal de apertura:** Si dicen "no" o "no funciona bien" → oportunidad directa.

---

## Objeción 2: "No quiero tocar SAP"

**Respuesta corta:**
> "Nosotros tampoco tocamos SAP. Solo leemos."

**Respuesta completa:**
> "Es exactamente la razón por la que el sistema se llama 'native read-only'. El extractor se conecta al Service Layer de SAP Business One usando las mismas APIs que usa la interfaz de SAP para consultar datos. No ejecuta scripts, no modifica tablas, no crea asientos."
>
> "El usuario SAP que necesitamos tiene permisos de solo lectura sobre OACT (plan de cuentas) y OJDT (libro diario). Literalmente el mismo permiso que tiene un usuario que solo puede consultar reportes."
>
> "¿Les preocupa algo específico de la conexión? ¿Tienen políticas de seguridad SAP que debamos conocer antes de empezar?"

**Señal de apertura:** Preguntar qué políticas tienen → los involucras en la solución.

---

## Objeción 3: "Me preocupa la seguridad"

**Respuesta corta:**
> "Bien que lo mencionen. Tenemos tres capas de seguridad y ninguna credencial viaja al navegador."

**Respuesta completa:**
> "La preocupación más común es: ¿alguien puede ver las credenciales SAP? La respuesta es no. Las credenciales del usuario SAP están almacenadas como SecretRef en el servidor de extracción — no en el dashboard, no en la base de datos, no en el código."
>
> "Segunda capa: cada empresa tiene sus datos completamente aislados. Usamos company_id en cada consulta — es imposible que los datos de una empresa aparezcan en el dashboard de otra."
>
> "Tercera capa: el extractor usa HTTPS y valida el certificado SSL del Service Layer de SAP. En producción no se permite conexión sin SSL válido."
>
> "¿Tienen un área de seguridad informática que quieran involucrar en la revisión técnica? Podemos hacer una sesión técnica específica con ellos."

---

## Objeción 4: "No confío en la clasificación contable"

**Respuesta corta:**
> "Bien. Por eso la clasificación la validamos con su contador antes del go-live."

**Respuesta completa:**
> "La clasificación contable es el corazón del producto y entendemos que no puede estar mal. Nadie conoce mejor el plan de cuentas de su empresa que su propio contador."
>
> "Lo que hacemos en el piloto: exportamos todas las cuentas del OACT de SAP y las revisamos línea por línea con el contador. Cada cuenta queda clasificada en la categoría correcta del P&L o Balance. Si hay cuentas ambiguas — y siempre hay algunas — el contador decide."
>
> "Después de esa sesión de clasificación, el sistema genera el P&L y el Balance con exactamente esas reglas. Si el contador cambia de opinión en alguna cuenta, el cambio se aplica en minutos y el dashboard se actualiza en el siguiente refresh."
>
> "¿Tienen disponible a su contador para una sesión de 2 horas durante la implementación?"

---

## Objeción 5: "¿Qué pasa si SAP cambia?"

**Respuesta corta:**
> "Las APIs de SAP Service Layer son estables y versionadas. Y si cambian, nosotros lo actualizamos."

**Respuesta completa:**
> "SAP Business One Service Layer es una API REST estándar que SAP mantiene con backward compatibility. Los objetos que usamos —OACT, OJDT, JDT1— son objetos core de SAP B1 que no han cambiado en versiones desde SAP 9.x hasta HANA."
>
> "Si SAP lanzara una nueva versión que cambiara el formato, eso sería un cambio de nuestro lado, no del cliente. Está cubierto dentro de la suscripción mensual."
>
> "En la práctica, lo que cambia entre versiones de SAP B1 no es el esquema contable sino funcionalidades de documentos operativos. El libro diario es el registro más estable del sistema."

---

## Objeción 6: "¿Por qué pagar mensual?"

**Respuesta corta:**
> "Porque la operación es continua: extracción diaria, monitoreo, actualizaciones, soporte."

**Respuesta completa:**
> "Entiendo la pregunta — ¿por qué no es un pago único y listo? La respuesta es que el valor real del producto no es la implementación inicial sino la operación diaria confiable."
>
> "Cada noche el extractor corre, trae los nuevos asientos de SAP, actualiza el MART, refresca el dashboard. Si algo falla, nosotros lo detectamos y lo solucionamos. Si SAP sube una actualización, nosotros lo ajustamos. Si el contador reclasifica una cuenta, nosotros lo aplicamos."
>
> "El modelo mensual también les protege: si en algún momento el servicio no les sirve, no están atrapados en un contrato de 2 años. Pueden salir con 30 días de aviso."
>
> "¿Prefieren explorar un modelo de contrato anual con descuento?"

---

## Objeción 7: "¿Cuánto demora?"

**Respuesta corta:**
> "10 días hábiles desde que nos dan acceso SAP."

**Respuesta completa:**
> "El cronograma real del piloto es:"
>
> - Día 1: credenciales SAP, validación de acceso
> - Día 2: configuración del perfil de conexión, test end-to-end
> - Días 3–4: extracción OACT + OJDT + JDT1, primera carga
> - Días 5–6: clasificación contable con el contador
> - Días 7–8: validación financiera — P&L, Balance, EBITDA contra reportes SAP
> - Día 9: capacitación de usuarios
> - Día 10: entrega formal, runbook operativo, go-live
>
> "Lo que puede extender ese plazo no es el sistema sino la disponibilidad del contador para la sesión de clasificación y la aprobación del acceso SAP por parte de TI. Por eso pedimos que esos dos actores estén disponibles desde el día 1."

---

## Objeción 8: "¿Esto reemplaza al contador?"

**Respuesta corta:**
> "No. El contador sigue siendo el contador. Nosotros le ahorramos horas de trabajo manual."

**Respuesta completa:**
> "El sistema no toma decisiones contables. No clasifica cuentas sin que el contador lo apruebe. No cierra libros. No emite estados financieros con valor legal."
>
> "Lo que sí hace es eliminar el trabajo manual de exportar SAP a Excel, calcular sumas, construir el P&L, verificar que el balance cuadre. Ese trabajo — que puede tomar varios días al mes — lo hace el sistema en minutos."
>
> "El contador sigue siendo responsable de la correcta clasificación de cuentas, de los ajustes contables, del cierre mensual. Pero ahora tiene un dashboard que le muestra el estado en tiempo real para que pueda detectar errores antes del cierre, no después."
>
> "¿Su contador está abierto a herramientas que le ahorren trabajo manual?"

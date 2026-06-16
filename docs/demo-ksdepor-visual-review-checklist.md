# Demo KSDEPOR — Visual Review Checklist

Sprint 8O — Junio 2026

Checklist rápido para antes de la demo. Cada item debe estar marcado como ✅ antes de comenzar.

---

## Pre-demo (técnico)

- [ ] API corriendo en `http://localhost:5103/swagger` → StatusCode 200
- [ ] Frontend corriendo en `http://localhost:5173`
- [ ] Transform ejecutado recientemente (dentro de las 2h antes de la demo)
- [ ] OPDN último run: SUCCESS
- [ ] Browser limpio: no hay errores en consola (F12)
- [ ] Browser en pantalla completa o 1280px mínimo

---

## Pantalla: Login

- [ ] Pantalla de login carga correctamente
- [ ] Login exitoso con credenciales de demo
- [ ] Redirige al portal con sidebar completo

---

## Pantalla: Ventas

- [ ] Header title = "Ventas" con badge "Native BI"
- [ ] KPI cards con valores (no "—" ni skeleton sin resolver)
- [ ] Tab Clientes muestra tabla con filas
- [ ] Tab Fulfillment muestra tabla con tasas coloreadas
- [ ] Sin errores en consola

---

## Pantalla: Compras

- [ ] Header title = "Compras"
- [ ] KPI cards con valores
- [ ] Tab Proveedores con tabla
- [ ] Tab Recepciones con tabla (o empty state con mensaje claro)
- [ ] Sin errores en consola

---

## Pantalla: Inventario

- [ ] Header title = "Inventario"
- [ ] KPI cards: Alta/Normal/Baja/Sin movimiento con conteos
- [ ] Tab Rotación: badges de color por categoría de rotación
- [ ] Tab Almacenes: tabla o empty state claro
- [ ] Sin errores en consola

---

## Pantalla: Finanzas

- [ ] Header title = "Finanzas"
- [ ] KPI cards con montos en CLP
- [ ] Tab AR: tabla de clientes con montos vencidos
- [ ] Montos > 0 en rojo
- [ ] Sin errores en consola

---

## Pantalla: Operaciones

- [ ] Header title = "Operaciones"
- [ ] Health score visible (no "—")
- [ ] Status del extractor y transform con color correcto
- [ ] Tab Alertas: tabla o "Sin alertas activas. El pipeline no presenta alertas pendientes."
- [ ] Sin errores en consola

---

## Sidebar

- [ ] Todas las secciones del sidebar visibles: Ventas, Compras, Inventario, Finanzas, Operaciones
- [ ] Item activo diferenciado con fondo
- [ ] No hay overflow ni elementos cortados

---

## Resultado

| Pantallas OK | Acción |
|---|---|
| 5/5 | ✅ Listo para demo |
| 4/5 | ⚠️ Verificar la que falló — puede continuar si es menor |
| <4/5 | ❌ No hacer demo — resolver primero |

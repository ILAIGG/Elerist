# Plan de implementación - Sistema elemental

## Objetivo

Incorporar elementos a habilidades y enemigos, ventajas y desventajas elementales, acumulación de efectos y reacciones, una habilidad de Agua, y mostrar el elemento en las cartas de mejora.

## Diseño base

Los elementos representan una propiedad independiente de los efectos de estado. Una habilidad aplica daño y, en fases posteriores, acumula su elemento en el objetivo. Las reacciones se resolverán a partir de los elementos acumulados, en lugar de que cada habilidad cree directamente una alteración concreta.

Elementos previstos:

- Neutral
- Fuego
- Agua
- Tierra
- Viento
- Planta
- Rayo
- Hielo

## Fases

### Fase 1 - Modelo elemental y efectividad

- [x] Crear el enum central de elementos.
- [x] Crear una tabla consultable de multiplicadores.
- [x] Asignar elemento a enemigos y jefe.
- [x] Asignar elemento a las habilidades y proyectiles existentes.
- [x] Centralizar el daño elemental para aplicar ventajas y desventajas.
- [x] Implementar las primeras relaciones: Fuego es débil contra Agua y fuerte contra Planta.
- [ ] Agregar pruebas específicas de la tabla y del daño modificado.

### Fase 2 - Acumulación elemental

- [x] Crear acumuladores de elemento por enemigo.
- [x] Definir intensidad, duración, consumo y reemplazo de acumulaciones.
- [x] Separar acumulación elemental de efectos de estado como `FrozenEffect`.
- [x] Preparar eventos para que el sistema de reacciones pueda observar nuevas acumulaciones.

### Fase 3 - Reacciones

- [x] Crear definiciones y resolución de reacciones.
- [x] Implementar Vaporización: Fuego + Agua.
- [x] Implementar Congelación: Agua + Hielo.
- [x] Conectar Congelación con el efecto de inmovilización existente.
- [ ] Dejar preparada la extensión para Quemadura, Electrocutación, Embarrado y demás reacciones anotadas.

### Fase 4 - Habilidad de Agua

- [x] Crear la habilidad/proyectil de Agua.
- [x] Asignarle `Water` y conectarla al sistema de acumulación.
- [x] Agregar sus estadísticas y mejoras.
- [x] Agregar su escena.
- [ ] Agregar sus traducciones.

### Fase 5 - Elemento en las cartas

- [x] Agregar elemento al modelo `Upgrade`.
- [x] Agregar el label de elemento a `UpgradeCard`.
- [x] Traducir los nombres de elementos desde `translations.csv`.
- [x] Asignar elementos a todas las mejoras de habilidades.
- [x] Usar `Neutral` para mejoras sin afinidad elemental.

### Fase 6 - Limpieza de traducciones y UpgradeSystem

- [x] Eliminar nombres y descripciones visibles hardcodeados en `UpgradeSystem`.
- [x] Mantener únicamente claves de traducción y argumentos dinámicos.
- [x] Migrar descripciones dinámicas a claves completas en `translations.csv`.
- [ ] Revisar y eliminar APIs redundantes como `GetDescription` cuando ya no sean necesarias.
- [ ] Reactivar o rediseñar `Permafrost` cuando Congelación esté implementada como reacción.

## Estado actual

Las Fases 1 a 5 y la migración principal de traducciones de la Fase 6 están implementadas. El modelo elemental, la tabla de efectividad, el daño elemental, la acumulación, las reacciones, Water Bolt y la visualización del elemento en las cartas están activos. Queda pendiente revisar si se elimina el selector de descripciones alternativas y reactivar o rediseñar `Permafrost` cuando corresponda.

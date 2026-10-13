# Elerist - Changelog

## [0.6.1] LevelUp Protype
- Se creó una "mini-versión" para el evento "LevelUp"
- Se añadió un sistema de clasificación el cual registra los nombres y la puntación de los jugadores
- El sistema de clasificación se ordena automáticamente teniendo en cuenta la cantidad de puntos que consiguó un jugador
- Los enemigos, el jefe y las mejoras obtenidas por el jugador dan puntos para el sistema de clasificación

## [0.6.0] - Frenzy Update
- Se cambió el nombre del juego: Elementalist -> Elerist
- Ahora la vida, nivel, experiencia y UpgradeCards se guardan entre niveles; estas se mantienen durante toda la run.
- Ahora se puede acceder al inventario desde el mapa de nodos.
- Se añadió un nuevo tipo de enemigo: FlyingEnemy. A diferencia de los enemigos comunes, este enemigo puede volar a través de los obstáculos del mapa.
- Ahora aparecen los elementos con un color correspondiente en las UpgradeCards y Damage Display
- Se añadió un sistema que elimina a los enemigos estén lejos del jugador y los aparece de nuevo cerca del mismo.
- Se modificaron los mapas para evitar que el jugador se quede quieto en una zona y "atore" a los enemigos.
- Se añadieron nuevos enemigos:
    - Nightmare: Es igual al enemigo que ya estaba en el juego, solo que con un diseño diferente y con un "nombre oficial"
    - Range Nightmare: A diferencia del Nihgtmare normal, este tiene un cañón el cual le permite atacar a distancia.
    - Flying Nightmare: A diferencia del Nightmare normal, este puede volar, permitiéndole pasar por sobre obstáculos mucho más facilmente.
- Se añadió un nuevo mapa "BossBattle", una arena circular donde se luchará contra el jefe actual.
- Ahora el jefe aparece en un punto específico del mapa en lugar del sistema anterior.
- Se modificó al "jefe base" dej juego:
    - Se quitó su segunda fase
    - Ahora el jefe puede hacer un "dash", el cual daña al jugador.
    - Ahora el jefe tiene la habilidad "Lluvia de meteoros", la cual hace que cada cierto tiempo, el jefe se mueve hacia el centro del mapa y lanza meteoros tres veces de manera aleatoria en el mapa durante un tiempo determinado.
    - Se añadió una BossBar
    - Se redujo su vida: 7000 -> 5500
- Se modificó el menú principal, añadiendo el nuevo título

- Se modificó el sistema de Spawn de los enemigos.
- Se aumentó la velocidad de los enemigos: 80 -> 155
- Se disminuyó la velocidad que obtenian los enemigos por dificultad: 0.03 -> 0.005
- Se redujo la velocidad que obtenian los enemigos por dificultad de mapa: 1 -> 0.2
- Se redujo la vida que obtenian los enemigos por dificultad por la mitad
- Se disminuyó el intervalo de aparición de los enemigos: 3 -> 1.5 (aparecen más enemigos al inicio)
- Se redujo la cantidad que disminuye el intervalo de aparición por nivel de dificultad: 0.05 -> 0.02
- Se redujo la cantidad de enemigos que aparecen por dificultad: 0.45 -> 0.08
- Se redujo la ifluencia de dificultad base de 0.1 -> 0.05
- Se redujo eñ tiempo necesario para subir la dificultad en el nivel de 20s a 10s
- Se corrigió un bug relacionado a las colisiones entre los enemigos y el jugador
- Se corrigió un bug relacionado a las ventanas de "Victoria" y "Derrota"
- Se corrigieron algunas descripciones de UpgradeCards y errores de traducción.

## [0.5.33] - Sistema de Inventario
- Se añadió un botón de inventario en la parte derecha central de la Hud.
- Se añadió una ventana de Inventario la cual muestra las UpgradeCards obtenidas en la partida.
- Se creo un nuevo estilo para las UpgradeCards: ahora tienen un borde de color y muestran el elemento relacionado a la mejora.

## [0.5.30] - Adición de SFX, loops para canciones, menú de opciones y bugfixes
- Se añadió un menú de configuración. Este menú permite cambiar los diferentes volúmenes y remapear controles.
- Los diferentes volúmenes se pueden modificar por sliders o cambiando el valor numérico directamente.
- Las canciones ahora suenan en bucle.
- Ahora Meteor reproduce ExplosionEffect al impactar.
- SFX añadidos:
    - Hit_01
    - Fireball
    - FrostRay
    - RepulsionBurst
    - WaterBolt
    - Dash
    - FireNova
- Se redujo el daño de WaterBolt: 20 -> 6
- Se corrigió un bugfix muy específico del menú de pausa.


## [0.5.23] - Música y SFX
- Se agregó un sistema de audio básico el cual permite reproducir SFX y Música a través de una librería de audios interna.
- Se agregaron los buses de audio "Master", "Music" y "SFX", los 3 configurables
- Se agregó un sistema que reduce el volumen de la música y aplica un filtro cuando se está en pausa.
- Se aplicaron varios filtros para evitar el clipping y distorsión cuando varios SFX suenan juntos a la vez.
- Se agregó un efecto de fade-in y fade-out para transicionar entre canciones.
- Ahora los botones hacen un sonido al hacerse click.
- SFX añadidos:
    - UIClick
    - Explosion
- Músicas añadidas:
    - R!ckes - creation
    - R!ckes - desert

## [0.5.15] - Correcciones de juego

### Arreglos importantes
- Se corrigió el desbloqueo de Water Bolt para que la mejora active la habilidad correctamente.
- Se separó Water Bolt de Fireball para que funcione como un proyectil propio con su elemento y comportamiento.
- Se revisó la lógica de daño elemental para que los proyectiles apliquen el elemento correcto.
- Se corrigió el feedback visual de las reacciones para que sean más fáciles de notar en combate.

### Mejoras de comodidad
- Las reacciones y los efectos visuales ahora aparecen de forma más clara cuando una habilidad interactúa con un enemigo.
- Se ajustó la consistencia entre las mejoras, la lógica del jugador y las artes disponibles.

## [0.5.9] - Textos y localización

- Se revisaron todas las descripciones de mejoras para que usen texto localizado en lugar de textos sueltos del código.
- Se mejoró la claridad de nombres, tipos y descripciones de artes y mejoras.
- Se actualizó la información mostrada en cartas y pantallas para que todo quede consistente en español e inglés.
- Se dejó una base más limpia para futuras mejoras sin perder la experiencia del jugador.

## [0.5.5] - Reacciones elementales

- Se añadieron las reacciones elementales de combate: Vaporización y Congelación.
- El fuego y el agua ahora pueden combinarse para crear una reacción explosiva y desorientante (Caporización).
- El agua y el hielo ahora pueden congelar a los enemigos de forma temporal (Congelación).
- Las reacciones tienen un efecto visible en combate para que se entiendan mejor durante la lucha.
- Los enemigos pueden mostrar señales visuales de estar congelados o vaporizados cuando reaccionan.

## [0.5.0] - Elemental System

### Elementos
- Se añadió un sistema de elementos al combate para que las artes interactúen entre sí de forma más rica.
- Los enemigos y los ataques ya tienen identidad elemental, lo que permite diferentes interacciones según el tipo de daño.
- Se mejoró la progresión del juego con un conjunto de mejoras elementales y artes más variadas.
- Nuevos elementos:
    - Fuego
    - Agua
    - Hielo
    - Tierra

### Artes
- Nuevo arte: Water Bolt. Su funcionamiento es similar a Fireball, solo que su elemento es de agua.

### Balance
- Frost Ray ya no aplica el efecto "congelación".
- Se eliminó la UpgradeCard "Permafrost"

## [0.4.4] - Idiomas

- Se añadió soporte completo para jugar en inglés o español.
- El idioma elegido se recuerda entre partidas.
- Se revisaron los textos del menú, tutorial, mapa, mejoras y pantallas de juego.

## [0.4.0] Journey Update - Part II

### Partidas

- Ahora puedes tener una única partida activa, guardada automáticamente.
- Puedes continuar tu partida desde el menú principal.
- Iniciar una partida nueva muestra una confirmación antes de reemplazar la partida actual.
- Tu progreso se conserva al salir y volver al juego.

### Mapa de aventura

- Se añadió un mapa roguelite con varios caminos posibles.
- Cada partida genera un mapa diferente con bifurcaciones, rutas y niveles variados.
- Solo se desbloquean los destinos conectados al nodo que acabas de completar.
- Puedes volver al mapa y continuar tu ruta desde donde la dejaste.

### Desafío

- La dificultad aumenta a medida que avanzas por el mapa.
- Los primeros niveles se centran en sobrevivir.
- El último nivel termina con un combate contra el jefe final.
- Completar el último nivel marca la partida como terminada.

### Interfaz

- El menú principal permite comenzar una partida nueva o continuar la actual.
- Se añadieron confirmaciones para las acciones que pueden hacerte perder progreso.
- El mapa muestra con más claridad los caminos y los nodos disponibles.
- La cámara del mapa conserva su posición al entrar y salir de los niveles.
- Se añadió una pantalla de finalización al completar la aventura.

## [0.3.0] - Journey Update - Part I

- Primera versión del mapa roguelite de Elerist.

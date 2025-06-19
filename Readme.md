# 🧑‍💻 WallE Programming Environment - Segundo Proyecto de Primer Año  
**Universidad de La Habana - Facultad de Matemática y Computación**  
*Desarrollado con Godot 4.4.1*  
---

## 🌟 Características Principales

### 🎨 Sistema de Cuadrícula Dinámica

# Ejemplo de creación de arte digital

Spawn(100, 100)

Color("Purple")

radio <- 50

loop_espiral

DrawCircle(1, 1, radio)

radio <- radio - 5

GoTo[loop_espiral](radio > 10)

## Dimensiones soportadas:

Mínimo: 16×16 píxeles

Máximo: 400×400 píxeles

Valor por defecto: 200×200

Sistema de coordenadas cartesianas (eje X horizontal, eje Y vertical)

Zoom adaptable con scroll del ratón

Posicionamiento preciso con coordenadas enteras

## 💾 Gestión de Proyectos
Guardado y carga de programas en formato .pw

Historial de archivos recientes

Editor de código con resaltado de sintaxis

## 🔧 Comandos Disponibles

| **Comando**                | **Descripción**                       | **Ejemplo**                |
| -------------------------- | ------------------------------------- | -------------------------- |
| `Spawn(x,y)`               | Posiciona a WallE en (x,y)            | `Spawn(0,0)`               |
| `Color("nombre")`          | Cambia color del pincel               | `Color("Blue")`            |
| `Size(n)`                  | Establece tamaño del pincel (1-10)    | `Size(3)`                  |
| `DrawLine(dx,dy,len)`      | Dibuja línea en dirección (dx,dy)     | `DrawLine(1,0,10)`         |
| `DrawCircle(dx,dy,r)`      | Dibuja círculo de radio r             | `DrawCircle(0,1,5)`        |
| `DrawRectangle(dx,dy,w,h)` | Dibuja rectángulo con dimensiones w×h | `DrawRectangle(1,0,20,10)` |
| `Fill()`                   | Rellena área conectada                | `Fill()`                   |
| `GoTo[etiqueta](cond)`     | Salto condicional                     | `GoTo[loop](contador<10)`  |

🔍 Funciones de Consulta
| **Función**                        | **Retorna**                                     |
| ---------------------------------- | ----------------------------------------------- |
| `GetActualX()`                     | Posición actual en X                            |
| `GetActualY()`                     | Posición actual en Y                            |
| `GetCanvasSize()`                  | Tamaño del lienzo (ej. 200)                     |
| `GetColorCount("col",x1,y1,x2,y2)` | Píxeles de color en área                        |
| `IsBrushColor("col")`              | 1 si el pincel es ese color, 0 si no            |
| `IsBrushSize(n)`                   | 1 si el tamaño es n, 0 si no                    |
| `IsCanvasColor("col",dx,dy)`       | 1 si el lienzo tiene color en posición relativa |

## ⚠️ Consideraciones Importantes
## 🔢 Trabajo Exclusivo con Enteros
El sistema solo soporta valores enteros en todas sus operaciones:
# Operaciones válidas
valor <- 5 + 3 * 2  # valor = 11

posY <- GetActualY() / 2  # División entera: 10/2=5

# Operaciones inválidas
z <- 3.14  # ERROR: No soporta decimales

## 📐 Límites de la Cuadrícula
Todas las coordenadas deben estar dentro de los límites configurados:
Spawn(500, 500)  # ERROR: Fuera de rango (máximo 400)
DrawLine(0,1,450) # ERROR: Excede tamaño máximo

## 🚫 Palabras Reservadas
Estas palabras no pueden usarse como nombres de variables:
Spawn, Color, Size, DrawLine, DrawCircle, DrawRectangle, Fill, 
GoTo, GetActualX, GetActualY, GetCanvasSize, GetColorCount,
IsBrushColor, IsBrushSize, IsCanvasColor

Ejemplo de error:
Size <- 5  # ERROR: Size es palabra reservada

Color <- 3 # ERROR: Color es palabra reservada

## 🚀 Extensibilidad del Proyecto
## 🧩 Arquitectura Modular
Lexer → Parser → Interpreter → CanvasRenderer → Godot Engine
    
El sistema está diseñado con módulos independientes que permiten:

Añadir nuevos comandos fácilmente

Implementar funciones adicionales

Extender el sistema de tipos

Mejorar el renderizado visual

## 🔌 Módulos Futuros
| **Módulo**                | **Estado**  | **Descripción**                      |
| ------------------------- | ----------- | ------------------------------------ |
| Funciones trigonométricas | En progreso | `Sin()`, `Cos()`, `Tan()`            |
| Soporte para texto        | Planeado    | Renderizado de fuentes y etiquetas   |
| Animaciones               | Idea        | Secuencias de movimiento programado  |
| Capas múltiples           | Idea        | Sistema de overlays y composición    |
| Depurador visual          | Backlog     | Paso a paso con inspección de estado |

## 🛠️ Instalación y Uso
Requisitos previos:

Godot Engine 4.4.1 o superior

.NET SDK 6.0 o superior (para compilación C#)

Clonar el repositorio:
git clone https://github.com/DanielColla/Wall-E-Pixerart.git
cd wall-e

Abrir en Godot:

Iniciar Godot

Seleccionar "Importar proyecto"

Navegar al directorio clonado

Abrir project.godot

Ejecutar el proyecto:

Ejecutar la escena principal Main.tscn

Escribir código en el editor

Presionar "Ejecutar" para ver los resultados



## 🎓 Objetivos Académicos
Este proyecto cumple con los objetivos de:

Implementar lenguajes de dominio específico (DSL)

Diseñar sistemas de interpretación de código

Dominar renderizado 2D programático

Practicar gestión de estado complejo

Desarrollar sistemas extensibles y mantenibles

## 🤝 ¡Contribuciones Bienvenidas!
¿Quieres mejorar WallE? ¡Abre un PR!

 Implementar funciones trigonométricas

 Añadir sistema de animaciones

 Mejorar manejo de errores

 Extender el sistema de variables

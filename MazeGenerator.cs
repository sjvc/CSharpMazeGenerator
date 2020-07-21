using System.Collections;
using System.Collections.Generic;
using System;

/*
    Esta clase genera un laberinto usando el algoritmo de Prim en C#
    Más info del algoritmo de Prim en: 
     - https://nopointer.wordpress.com/2015/10/04/generar-laberinto-aleatorio-con-el-algoritmo-de-prims
     - https://en.wikipedia.org/wiki/Maze_generation_algorithm
     - https://stackoverflow.com/questions/29739751/implementing-a-randomly-generated-maze-using-prims-algorithm
*/
public class MazeGenerator {
    // El número de filas y columnas ha de ser impar
    public const int MAX_ROWS = 101;
    public const int MAX_COLS = 101;

    // Creamos el laberinto con el tamaño máximo, así evitamos crear/destruir memoria y evitamos el lag del recolector de basura
    private MazeCell[,] maze = new MazeCell[MAX_ROWS, MAX_COLS];

    // Tamaño del laberinto actual
    private int rows, cols;

    // Celda inicial
    public MazeCellIndex startingCellIndex {get; private set;}

    // Tablas temporales de trabajo para el proceso
    private List<MazeCellIndex> lockedNeighbours = new List<MazeCellIndex>();
    private List<MazeCellIndex> unlockedNeighbours = new List<MazeCellIndex>();

    // Generación de números aleatorios
    private Random random = new Random();

    // Si es true se cancelará el proceso
    private bool cancel = false;

    // Evento ejecutado cada vez que se cambia el tipo de celda durante la generación
    public event Action<MazeCellIndex> OnMazeCellTypeChanged;

    public void Initialize(int numRows, int numCols) {
        if (numRows > MAX_ROWS || numCols > MAX_COLS || numRows < 1 || numCols < 1) {
            throw new System.ArgumentException("El número de filas o columnas no es correcto para generar el laberinto");
        }

        if (numRows %2 == 0 || numCols%2 == 0) {
            throw new System.ArgumentException("El número de filas y columnas ha de ser impar");
        }

        rows = numRows;
        cols = numCols;

        // Calcular celda de inicio
        startingCellIndex = GetStartingCell();

        // Al inicio todas las celdas están bloqueadas
        for (int i=0; i<MAX_ROWS; i++) {
            for (int j=0; j<MAX_COLS; j++) {
                maze[i, j] = new MazeCell(MazeCellType.LOCKED, false);
            }
        }
    }

    private MazeCellIndex GetStartingCell() {
        // Podemos establecer aquí las celdas de inicio que queramos
        int row = 1;
        int col = (int) Math.Floor(cols / 2f);

        // Para que los bordes del laberinto se queden como celdas bloqueadas, entre la posición de inicio
        // y el borde del laberinto ha de haber un número impar de celdas (o ninguna)
        if (row % 2 == 0) row += random.Next(2) == 0 ? -1 : 1;
        if (col % 2 == 0) col += random.Next(2) == 0 ? -1 : 1;

        return new MazeCellIndex(row, col);
    }

    public void Cancel(){
        cancel = true;
    }

    public int GetNumColumns() {
        return cols;
    }

    public int GetNumRows() {
        return rows;
    }

    public MazeCellType? GetCellType(int row, int col, bool invertRowsAndCols = false) {
        int x = invertRowsAndCols ? col : row;
        int y = invertRowsAndCols ? row : col;

        if (MazeContainsCell(x, y)) {
            return maze[x, y].type;
        }

        return null;
    }

    public void Generate() {
        cancel = false;

        // La celda de inicio es startingCellIndex. Se añaden sus vecinos bloqueados al array y se desbloquea.
        AddNeighboursToList(startingCellIndex, lockedNeighbours, MazeCellType.LOCKED);
        SetMazeCell(startingCellIndex, MazeCellType.UNLOCKED);
        
        while (!cancel && lockedNeighbours.Count > 0) {
            // Obtener una celda bloqueada al azar
            MazeCellIndex randomLockedCell = lockedNeighbours[random.Next(lockedNeighbours.Count)];

            // Si aún no he visitado la celda bloqueada
            if (!maze[randomLockedCell.row, randomLockedCell.col].visited) {
                // La marco como visitada
                maze[randomLockedCell.row, randomLockedCell.col].visited = true;

                // Obtener un vecino desbloqueado al azar de la celda bloqueada anterior
                AddNeighboursToList(randomLockedCell, unlockedNeighbours, MazeCellType.UNLOCKED);
                MazeCellIndex randomUnlockedCell = unlockedNeighbours[random.Next(unlockedNeighbours.Count)];
                unlockedNeighbours.Clear();

                // Desbloquear celda intermedia entre las celdas anteriores
                MazeCellIndex betweenCellIndex = new MazeCellIndex((randomLockedCell.row + randomUnlockedCell.row) / 2, (randomLockedCell.col + randomUnlockedCell.col) / 2);
                SetMazeCell(betweenCellIndex,  MazeCellType.UNLOCKED);

                // Guardar vecinos bloqueados de randomLockedCell
                AddNeighboursToList(randomLockedCell, lockedNeighbours, MazeCellType.LOCKED);

                // No desbloqueo celdas que estén en el borde del laberinto
                // if (!CellIsAtEdge(randomLockedCell.row, randomLockedCell.col)) {
                    // Desbloquear la celda bloqueada
                    SetMazeCell(randomLockedCell, MazeCellType.UNLOCKED);
                // }
            }

            // Quitar la celda bloqueada del array de bloqueadas
            lockedNeighbours.Remove(randomLockedCell);
        }

        // Vaciar array de bloqueadas (por si se ha cancelado la ejecución)
        lockedNeighbours.Clear();
    }

    private void SetMazeCell(MazeCellIndex index, MazeCellType type) {
        maze[index.row, index.col].type = type;

        // Ejecutar evento
        if (OnMazeCellTypeChanged != null) {
            OnMazeCellTypeChanged(index);
        }
    }

    // Añade los 4 vecinos con distancia de 2 a la lista list, si son del tipo type (null si no importa el tipo)
    private void AddNeighboursToList(MazeCellIndex cell, List<MazeCellIndex> list, MazeCellType? type = null) {
        AddNeighbourToList(cell,  2,  0, list, type); // Top
        AddNeighbourToList(cell,  0,  2, list, type); // Right
        AddNeighbourToList(cell, -2,  0, list, type); // Bottom
        AddNeighbourToList(cell,  0, -2, list, type); // Left
    }

    // Añade el vecino con distancia [rowDelta, colDelta] a la lista list, si es del tipo type (null si no importa el tipo)
    private void AddNeighbourToList(MazeCellIndex cell, int rowDelta, int colDelta, List<MazeCellIndex> list, MazeCellType? type = null) {
        if (MazeContainsCell(cell.row + rowDelta, cell.col + colDelta) && (type == null || type == maze[cell.row + rowDelta, cell.col + colDelta].type)) {
            list.Add(new MazeCellIndex(cell.row + rowDelta, cell.col + colDelta));
        }
    }

    // Devuelve true si la celda row, col está dentro del laberinto
    public bool MazeContainsCell(int row, int col) {
        return row >= 0 && row < rows && col >= 0 && col < cols;
    }

    // Devuelve true si es una celda que está en el borde del laberinto
    public bool CellIsAtEdge(int row, int col) {
        return row == 0 || col == 0 || row == rows-1 || col == cols-1;
    }
}

// Tipo de celda -> bloqueada o desbloqueada
public enum MazeCellType : int {
    LOCKED   = 0,
    UNLOCKED = 1
}

// Estructura que representa una celda del laberinto
public struct MazeCell {
    public MazeCell(MazeCellType pType, bool pVisited) {
        type = pType; // Bloqueado / Desbloqueado
        visited = pVisited; // Indica si se ha visitado esa celda en la generación, para no volverla a visitar
    }

    public MazeCellType type;
    public bool visited;
}

// Estructura para guardar el índice de una celda
public struct MazeCellIndex {
    public MazeCellIndex(int pRow, int pCol) {
        row = pRow;
        col = pCol;
    }

    public int row;
    public int col;
}
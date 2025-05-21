using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Text.Json;
using System.Net.NetworkInformation;

namespace cli_life
{

    public class GameConfig
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int CellSize { get; set; }
        public double LiveDensity { get; set; }
        public GameConfig(int Width, int Height, int CellSize, double LiveDensity)
        {
            this.Width = Width;
            this.Height = Height;
            this.CellSize = CellSize;
            this.LiveDensity = LiveDensity;
        }
    }
    public class Figure
    {
        public string Name { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int[][] Cells { get; set; }

    }

    public class Cell
    {
        public bool IsAlive;
        public readonly List<Cell> neighbors = new List<Cell>();
        private bool IsAliveNext;
        public void DetermineNextLiveState()
        {
            int liveNeighbors = neighbors.Where(x => x.IsAlive).Count();
            if (IsAlive)
                IsAliveNext = liveNeighbors == 2 || liveNeighbors == 3;
            else
                IsAliveNext = liveNeighbors == 3;
        }
        public void Advance()
        {
            IsAlive = IsAliveNext;
        }
    }
    public class Board
    {
        public readonly Cell[,] Cells;
        public readonly int CellSize;

        public int Columns { get { return Cells.GetLength(0); } }
        public int Rows { get { return Cells.GetLength(1); } }
        public int Width { get { return Columns * CellSize; } }
        public int Height { get { return Rows * CellSize; } }

        public Board(int width, int height, int cellSize, double liveDensity = .1)
        {
            CellSize = cellSize;

            Cells = new Cell[width / cellSize, height / cellSize];
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    Cells[x, y] = new Cell();

            ConnectNeighbors();
            Randomize(liveDensity);
        }

        readonly Random rand = new Random();
        public void Randomize(double liveDensity)
        {
            foreach (var cell in Cells)
                cell.IsAlive = rand.NextDouble() < liveDensity;
        }

        public void Advance()
        {
            foreach (var cell in Cells)
                cell.DetermineNextLiveState();
            foreach (var cell in Cells)
                cell.Advance();
        }
        private void ConnectNeighbors()
        {
            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    int xL = (x > 0) ? x - 1 : Columns - 1;
                    int xR = (x < Columns - 1) ? x + 1 : 0;

                    int yT = (y > 0) ? y - 1 : Rows - 1;
                    int yB = (y < Rows - 1) ? y + 1 : 0;

                    Cells[x, y].neighbors.Add(Cells[xL, yT]);
                    Cells[x, y].neighbors.Add(Cells[x, yT]);
                    Cells[x, y].neighbors.Add(Cells[xR, yT]);
                    Cells[x, y].neighbors.Add(Cells[xL, y]);
                    Cells[x, y].neighbors.Add(Cells[xR, y]);
                    Cells[x, y].neighbors.Add(Cells[xL, yB]);
                    Cells[x, y].neighbors.Add(Cells[x, yB]);
                    Cells[x, y].neighbors.Add(Cells[xR, yB]);
                }
            }
        }
        public void SaveState(string filePath)
        {
            File.WriteAllText(filePath, string.Empty);
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                for (int y = 0; y < Rows; y++)
                {
                    StringBuilder line = new StringBuilder();
                    for (int x = 0; x < Columns; x++)
                    {
                        line.Append(Cells[x, y].IsAlive ? '1' : '0');
                    }
                    writer.WriteLine(line.ToString());
                }
            }
        }
        public static Board LoadFromTextFile(string filePath, int cellSize = 1)
        {
            string[] lines = File.ReadAllLines(filePath);
            int height = lines.Length;
            int width = lines[0].Length;

            bool[,] initialState = new bool[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    initialState[x, y] = lines[y][x] == '1';
                }
            }

            return new Board(width * cellSize, height * cellSize, cellSize, initialState);
        }
        public Board(int width, int height, int cellSize, bool[,] initialState)
        {
            CellSize = cellSize;
            Cells = new Cell[width / cellSize, height / cellSize];

            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    Cells[x, y] = new Cell();

            ConnectNeighbors();


            for (int x = 0; x < Math.Min(Columns, initialState.GetLength(0)); x++)
                for (int y = 0; y < Math.Min(Rows, initialState.GetLength(1)); y++)
                    Cells[x, y].IsAlive = initialState[x, y];
        }
        public int CountLiveCells()
        {
            int count = 0;
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    if (Cells[x, y].IsAlive)
                        count++;
            return count;
        }
        public int CountClusters()
        {
            int count = 0;
            bool[,] visited = new bool[Columns, Rows];

            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    if (Cells[x, y].IsAlive && !visited[x, y])
                    {
                        count++;
                        BFS(x, y, ref visited);
                    }
                }
            }
            return count;
        }
        private List<(int, int)> BFS(int startX, int startY, ref bool[,] visited, bool trackCells = false)
        {
            var cluster = trackCells ? new List<(int, int)>() : null;
            Queue<(int, int)> queue = new Queue<(int, int)>();

            queue.Enqueue((startX, startY));
            visited[startX, startY] = true;
            cluster?.Add((startX, startY));

            while (queue.Count > 0)
            {
                var (currentX, currentY) = queue.Dequeue();
                var neighbors = GetNeighbors(currentX, currentY);

                foreach (var (dx, dy) in neighbors)
                {
                    if (Cells[dx, dy].IsAlive && !visited[dx, dy])
                    {
                        visited[dx, dy] = true;
                        queue.Enqueue((dx, dy));
                        cluster?.Add((dx, dy));
                    }
                }
            }

            return cluster;
        }
        public List<(int x, int y)> GetNeighbors(int x, int y)
        {
            int xL = (x > 0) ? x - 1 : Columns - 1;
            int xR = (x < Columns - 1) ? x + 1 : 0;
            int yT = (y > 0) ? y - 1 : Rows - 1;
            int yB = (y < Rows - 1) ? y + 1 : 0;


            var neighbors = new List<(int, int)>(8);


            neighbors.Add((xL, yT));
            neighbors.Add((x, yT));
            neighbors.Add((xR, yT));
            neighbors.Add((xL, y));
            neighbors.Add((xR, y));
            neighbors.Add((xL, yB));
            neighbors.Add((x, yB));
            neighbors.Add((xR, yB));

            return neighbors;
        }

        public Dictionary<string, int> ClassifyClusters()
        {
            var clusterCounts = new Dictionary<string, int>();
            bool[,] visited = new bool[Columns, Rows];

            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    if (Cells[x, y].IsAlive && !visited[x, y])
                    {
                        var clusterCells = BFS(x, y, ref visited, trackCells: true);
                        var patternName = IdentifyPattern(clusterCells);
                        clusterCounts[patternName] = clusterCounts.GetValueOrDefault(patternName, 0) + 1;
                    }
                }
            }
            return clusterCounts;
        }
        private string IdentifyPattern(List<(int, int)> clusterCells)
        {
            var normalized = MoveCoordinatesIn00(clusterCells);

            foreach (var pattern in KnownPatterns)
            {
                if (normalized.SetEquals(pattern.Value))
                    return pattern.Key;
            }
            return "Other";
        }
        public HashSet<(int, int)> MoveCoordinatesIn00(List<(int, int)> cells)
        {
            if (cells.Count == 0) return new HashSet<(int, int)>();

            int minX = cells.Min(c => c.Item1);
            int minY = cells.Min(c => c.Item2);

            return new HashSet<(int, int)>(cells.Select(c => (c.Item1 - minX, c.Item2 - minY)));
        }
        private static readonly Dictionary<string, HashSet<(int, int)>> KnownPatterns = new()
        {
            { "Square", new HashSet<(int, int)> { (0,0), (1,0), (0,1), (1,1) } },
            { "Lake", new HashSet<(int, int)> { (0,1), (1, 0), (2, 0), (3, 1), (3, 2), (2, 3), (1, 3), (0, 2) } },
            { "Blinker", new HashSet<(int, int)> { (0,0), (1,0), (2,0) } },
            { "Glider", new HashSet<(int, int)> { (1,0), (2,1), (0,2), (1,2), (2,2) } },
        };
        public bool IsStable(int[] lastCellCounts, int stabilityThreshold)
        {
            if (lastCellCounts.Length < stabilityThreshold)
                return false;

            int firstValue = lastCellCounts[0];
            for (int i = 1; i < stabilityThreshold; i++)
            {
                if (lastCellCounts[i] != firstValue)
                    return false;
            }
            return true;
        }


    };

    class Program
    {
        static GameConfig LoadConfig(string configPath)
        {

            if (!File.Exists(configPath))
            {
                var defaultConfig = new GameConfig(50, 20, 1, 0.5);
                return defaultConfig;
            }
            else
            {
                string json = File.ReadAllText(configPath);
                Console.WriteLine("Ok");
                return JsonSerializer.Deserialize<GameConfig>(json);
            }

        }
        static Board board;
        static Dictionary<int, string> figureFiles = new Dictionary<int, string>
        {
            {1, @"C:\Users\armok\Documents\lebedeva\IASR\VSLife\Life\glider.json"},
            {2, @"C:\Users\armok\Documents\lebedeva\IASR\VSLife\Life\square.json"},
            {3, @"C:\Users\armok\Documents\lebedeva\IASR\VSLife\Life\lake.json"},
            {4, @"C:\Users\armok\Documents\lebedeva\IASR\VSLife\Life\gun.json"},
            {5, @"C:\Users\armok\Documents\lebedeva\IASR\VSLife\Life\eater.json"}
        };
        public static void LoadFigureToBoard(int figureNum, int posX, int posY)
        {
            if (!figureFiles.ContainsKey(figureNum))
            {
                Console.WriteLine("Неверный номер фигуры!");
                return;
            }

            string filePath = figureFiles[figureNum];
            string json = File.ReadAllText(filePath);
            Figure figure = JsonSerializer.Deserialize<Figure>(json);

            for (int y = 0; y < figure.Height; y++)
            {
                for (int x = 0; x < figure.Width; x++)
                {

                    int targetX = (posX + x) % board.Columns;
                    int targetY = (posY + y) % board.Rows;


                    if (targetX < 0) targetX += board.Columns;
                    if (targetY < 0) targetY += board.Rows;

                    board.Cells[targetX, targetY].IsAlive = figure.Cells[y][x] == 1;
                }
            }
        }
        static void AddFiguresMenu()
        {

            bool addingFigures = true;
            while (addingFigures)
            {
                Console.Clear();
                Console.WriteLine("Выберите фигуру:");
                Console.WriteLine("1 - Глайдер");
                Console.WriteLine("2 - Блок");
                Console.WriteLine("3 - Озеро");
                Console.WriteLine("4 - Ружьё");
                Console.WriteLine("5 - Пожиратель");
                Console.WriteLine("6 - Закончить ввод");

                if (int.TryParse(Console.ReadLine(), out int choice))
                {
                    if (choice == 6)
                    {
                        addingFigures = false;
                        continue;
                    }

                    if (choice >= 1 && choice <= 5)
                    {
                        Console.Write("Введите координату X (левый верхний угол): ");
                        if (int.TryParse(Console.ReadLine(), out int x))
                        {
                            Console.Write("Введите координату Y (левый верхний угол): ");
                            if (int.TryParse(Console.ReadLine(), out int y))
                            {
                                LoadFigureToBoard(choice, x, y);
                                Console.Clear();
                                Render();
                                Console.WriteLine("Фигура добавлена. Добавить ещё? (Y/N)");
                                if (Console.ReadLine().ToUpper() != "Y")
                                    addingFigures = false;
                            }
                        }
                    }
                }
            }
        }

        static void ClearBoard()
        {
            for (int x = 0; x < board.Columns; x++)
                for (int y = 0; y < board.Rows; y++)
                    board.Cells[x, y].IsAlive = false;
        }

        static private void Reset()
        {
            string configPath = @"C:\Users\armok\Documents\lebedeva\IASR\VSLife\Life\config.json";
            GameConfig config = LoadConfig(configPath);

            board = new Board(
               width: config.Width,
                height: config.Height,
                cellSize: config.CellSize,
                liveDensity: config.LiveDensity
            );
        }
        static void Render()
        {
            for (int row = 0; row < board.Rows; row++)
            {
                for (int col = 0; col < board.Columns; col++)
                {
                    var cell = board.Cells[col, row];
                    if (cell.IsAlive)
                    {
                        Console.Write('*');
                    }
                    else
                    {
                        Console.Write(' ');
                    }
                }
                Console.Write('\n');
            }
        }
        static void ShowMenu()
        {
            Console.WriteLine("Управление:");
            Console.WriteLine("Space - Пауза/Продолжить");
            Console.WriteLine("S - Сохранить текущее состояние");
            Console.WriteLine("L - Загрузить состояние");
            Console.WriteLine("F - Добавить фигуры");
            Console.WriteLine("A - Анализ текущего состояния");
            Console.WriteLine("E - Провести эксперимент стабильности");
            Console.WriteLine("ESC - Выход");
            Console.WriteLine("Нажмите клавишу для продолжения...");

        }
        static void AnalyzeBoard()
        {
            var clusters = board.CountClusters();
            var classification = board.ClassifyClusters();

            Console.WriteLine("\nАнализ доски:");
            Console.WriteLine($"Всего живых клеток: {board.CountLiveCells()}");
            Console.WriteLine($"Всего кластеров: {clusters}");
            Console.WriteLine("\nКлассификация кластеров:");
            foreach (var item in classification)
            {
                Console.WriteLine($"{item.Key}: {item.Value}");
            }
        }

        static void RunDensityExperiment()
        {


            int boardWidth = 50;
            int boardHeight = 20;
            int maxGenerations = 10000;
            int stabilityThreshold = 10;
            int experimentsPerDensity = 20;

            var results = new List<(double density, int stableGeneration)>();

            Console.WriteLine("Начало эксперимента с шагом плотности 0.01...");
            Console.WriteLine($"Для каждой плотности будет проведено {experimentsPerDensity} экспериментов");


            for (double density = 0.1; density <= 0.9; density += 0.01)
            {
                int totalStableGenerations = 0;
                int successfulExperiments = 0;


                for (int i = 0; i < experimentsPerDensity; i++)
                {
                    var board = new Board(boardWidth, boardHeight, 1, density);
                    var lastCounts = new int[stabilityThreshold];
                    int stableGeneration = -1;

                    for (int gen = 0; gen < maxGenerations; gen++)
                    {
                        board.Advance();
                        int count = board.CountLiveCells();

                        Array.Copy(lastCounts, 1, lastCounts, 0, stabilityThreshold - 1);
                        lastCounts[stabilityThreshold - 1] = count;

                        if (board.IsStable(lastCounts, stabilityThreshold))
                        {
                            stableGeneration = Math.Max(0, gen - stabilityThreshold); ;
                            totalStableGenerations += stableGeneration;
                            successfulExperiments++;
                            break;
                        }
                    }
                }


                int avgStableGeneration = successfulExperiments > 0
                    ? totalStableGenerations / successfulExperiments
                    : -1;

                results.Add((density, avgStableGeneration));
                Console.WriteLine($"Плотность: {density:F2}, Среднее поколение стабилизации: {avgStableGeneration}");
            }


            string fileName = @"C:\Users\armok\Documents\lebedeva\IASR\VSLife\Life\data.txt";
            using (StreamWriter writer = new StreamWriter(fileName))
            {
                writer.WriteLine("Density,StableGeneration");
                foreach (var result in results)
                {
                    writer.WriteLine($"{result.density:F2} {result.stableGeneration}");
                }
            }

            Console.WriteLine($"\nРезультаты сохранены в {fileName}");


        }


        static void Main(string[] args)
        {


            Reset();
            int generation = 0;
            bool isRunning = true;
            bool isPaused = false;

            while (isRunning)
            {
                if (generation == 0)
                {
                    ShowMenu();
                    Console.ReadKey();
                }
                if (!isPaused)
                {
                    Console.Clear();
                    Render();
                    board.Advance();
                    generation++;
                    Thread.Sleep(100);
                }

                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;

                    switch (key)
                    {
                        case ConsoleKey.Spacebar:
                            isPaused = !isPaused;
                            Console.Clear();
                            Render();
                            Console.WriteLine(isPaused ? "Пауза" : "Продолжение");
                            if (isPaused) ShowMenu();
                            break;

                        case ConsoleKey.S:
                            Console.Clear();
                            Render();
                            Console.Write("Введите путь файла для сохранения: ");
                            string saveFile = Console.ReadLine();
                            board.SaveState(saveFile);
                            Console.WriteLine($"Сохранено в {saveFile}. Нажмите любую клавишу...");
                            Console.ReadKey();
                            break;

                        case ConsoleKey.L:
                            Console.Clear();
                            Console.Write("Введите путь файла для загрузки: ");
                            string loadFile = Console.ReadLine();
                            if (File.Exists(loadFile))
                            {
                                board = Board.LoadFromTextFile(loadFile);
                                generation = 0;
                                Console.WriteLine($"Загружено из {loadFile}. Нажмите любую клавишу...");
                            }
                            else
                            {
                                Console.WriteLine("Файл не найден!");
                            }
                            Console.ReadKey();
                            break;



                        case ConsoleKey.Escape:
                            isRunning = false;
                            break;


                        case ConsoleKey.F:
                            ClearBoard();
                            AddFiguresMenu();
                            break;
                        case ConsoleKey.A:
                            AnalyzeBoard();
                            Console.WriteLine("Нажмите любую клавишу для продолжения...");
                            Console.ReadKey();
                            break;

                        case ConsoleKey.E:
                            RunDensityExperiment();
                            Console.WriteLine("Нажмите любую клавишу для продолжения...");
                            Console.ReadKey();
                            break;
                    }
                }
            }

            Console.WriteLine("игра завершена.");
        }
    }
}

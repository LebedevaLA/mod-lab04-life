using Xunit;
using cli_life;
using System.IO;
using System.Text.Json;
using Microsoft.VisualStudio.TestPlatform.TestHost;

public class LifeTests
{
    
    [Fact]
    public void Test_Board_Initialization()
    {
        var board = new Board(50, 20, 1, 0.5);
        Assert.Equal(50, board.Columns);
        Assert.Equal(20, board.Rows);
    }

    [Fact]
    public void Test_Board_Save_And_Load()
    {
        var board1 = new Board(10, 10, 1, 0.3);
        string testFile = "test_board.txt";
        board1.SaveState(testFile);

        var board2 = Board.LoadFromTextFile(testFile);
        Assert.Equal(board1.Columns, board2.Columns);
        Assert.Equal(board1.Rows, board2.Rows);

        File.Delete(testFile);
    }

    
    [Fact]
    public void Test_Figure_Loading()
    {
        string figureFile = "test_figure.json";
        var figure = new Figure
        {
            Name = "Test",
            Width = 3,
            Height = 3,
            Cells = new[] { new[] { 1, 0, 1 }, new[] { 0, 1, 0 }, new[] { 1, 0, 1 } }
        };
        File.WriteAllText(figureFile, JsonSerializer.Serialize(figure));

        var loaded = JsonSerializer.Deserialize<Figure>(File.ReadAllText(figureFile));
        Assert.Equal(figure.Name, loaded.Name);

        File.Delete(figureFile);
    }

    [Fact]
    public void Test_Cluster_Detection1()
    {
        var board = new Board(10, 10, 1, 0);
        // Создаем блок 2x2
        board.Cells[1, 1].IsAlive = true;
        board.Cells[1, 2].IsAlive = true;
        board.Cells[2, 1].IsAlive = true;
        board.Cells[2, 2].IsAlive = true;

        var clusters = board.FindClusters();
        Assert.Single(clusters);
        Assert.Equal(4, clusters[0].Count);
    }

    [Fact]
    public void Test_Stable_Detection()
    {
        var board = new Board(4, 4, 1, 0);
        // Создаем стабильный блок 2x2
        board.Cells[1, 1].IsAlive = true;
        board.Cells[1, 2].IsAlive = true;
        board.Cells[2, 1].IsAlive = true;
        board.Cells[2, 2].IsAlive = true;

        int[] counts = new int[10];
        for (int i = 0; i < 10; i++)
        {
            board.Advance();
            counts[i] = board.CountLiveCells();
        }

        // Блок должен быть стабильным - всегда 4 клетки
        Assert.True(board.IsStable(counts, 3));
    }

    [Fact]
    public void Test_Count_Live_Cells()
    {
        var board = new Board(5, 5, 1, 0);
        board.Cells[1, 1].IsAlive = true;
        board.Cells[2, 2].IsAlive = true;
        Assert.Equal(2, board.CountLiveCells());
    }

    [Fact]
    public void Test_Pattern_Classification()
    {
        var board = new Board(4, 4, 1, 0);
        // Создаем блок
        board.Cells[1, 1].IsAlive = true;
        board.Cells[1, 2].IsAlive = true;
        board.Cells[2, 1].IsAlive = true;
        board.Cells[2, 2].IsAlive = true;

        var clusters = board.FindClusters();
        var classification = board.ClassifyClusters(clusters);

        Assert.Equal(1, classification["Block"]);
    }

   

    [Fact]
    public void Test_Neighbor_Connections()
    {
        var board = new Board(3, 3, 1, 0);
        var center = board.Cells[1, 1];
        Assert.Equal(8, center.neighbors.Count); // У центральной клетки 8 соседей
    }
    [Fact]
    public void Test_DeadBoard_StaysDead()
    {
        // Пустая доска должна оставаться пустой
        var board = new Board(3, 3, 1, 0);
        board.Advance();
        Assert.Equal(0, board.CountLiveCells());
    }

    [Fact]
    public void Test_Block_IsStable()
    {
        // Блок 2x2 должен оставаться неизменным
        bool[,] initialState = new bool[4, 4];
        initialState[1, 1] = true;
        initialState[1, 2] = true;
        initialState[2, 1] = true;
        initialState[2, 2] = true;

        var board = new Board(4, 4, 1, initialState);
        board.Advance();

        Assert.True(board.Cells[1, 1].IsAlive);
        Assert.True(board.Cells[1, 2].IsAlive);
        Assert.True(board.Cells[2, 1].IsAlive);
        Assert.True(board.Cells[2, 2].IsAlive);
        Assert.Equal(4, board.CountLiveCells());
    }

    [Fact]
    public void Test_Glider_MovesCorrectly_With_Debug()
    {
        
        bool[,] initialState = new bool[20, 20];

       
        initialState[5, 6] = true;  // *
        initialState[6, 7] = true;  //  *
        initialState[7, 5] = true;  // ***
        initialState[7, 6] = true;
        initialState[7, 7] = true;

        var board = new Board(20, 20, 1, initialState);

       

        // Делаем 4 шага с отладкой
        for (int i = 0; i < 4; i++)
        {
            board.Advance();
            
        }

        // Проверяем конечное положение (должно сместиться на +1 по X и +1 по Y)
        Assert.True(board.Cells[6, 7].IsAlive, "Ячейка (6,7) должна быть живой");
        Assert.True(board.Cells[7, 8].IsAlive, "Ячейка (7,8) должна быть живой");
        Assert.True(board.Cells[8, 6].IsAlive, "Ячейка (8,6) должна быть живой");
        Assert.True(board.Cells[8, 7].IsAlive, "Ячейка (8,7) должна быть живой");
        Assert.True(board.Cells[8, 8].IsAlive, "Ячейка (8,8) должна быть живой");
    }
    [Fact]
    public void Test_Board_SaveAndLoad_Consistency()
    {
        // Сохранение и загрузка должны сохранять состояние
        var board1 = new Board(5, 5, 1, 0.3);
        string testFile = "test_save.txt";
        board1.SaveState(testFile);

        var board2 = Board.LoadFromTextFile(testFile);

        for (int x = 0; x < board1.Columns; x++)
        {
            for (int y = 0; y < board1.Rows; y++)
            {
                Assert.Equal(board1.Cells[x, y].IsAlive, board2.Cells[x, y].IsAlive);
            }
        }

        File.Delete(testFile);
    }

    [Fact]
    public void Test_RandomBoard_HasApproximateDensity()
    {
        // Проверка что рандомная доска имеет приблизительно правильную плотность
        double density = 0.3;
        var board = new Board(100, 100, 1, density);
        int liveCells = board.CountLiveCells();
        double actualDensity = liveCells / (double)(board.Columns * board.Rows);

        Assert.InRange(actualDensity, density - 0.05, density + 0.05);
    }

    [Fact]
    public void Test_Cluster_Detection()
    {
        // Проверка обнаружения кластеров (изолированных групп)
        bool[,] initialState = new bool[5, 5];
        initialState[1, 1] = true; // Кластер 1
        initialState[1, 2] = true;

        initialState[3, 3] = true; // Кластер 2
        initialState[3, 4] = true;
        initialState[4, 3] = true;

        var board = new Board(5, 5, 1, initialState);
        var clusters = board.FindClusters();

        Assert.Equal(2, clusters.Count);
        Assert.Equal(2, clusters[0].Count); // Кластер из 2 клеток
        Assert.Equal(3, clusters[1].Count); // Кластер из 3 клеток
    }

    [Fact]
    public void Test_Classification_Of_Known_Patterns()
    {
        // Проверка классификации известных паттернов
        bool[,] initialState = new bool[10, 10];

        // Добавляем блок
        initialState[1, 1] = true;
        initialState[1, 2] = true;
        initialState[2, 1] = true;
        initialState[2, 2] = true;

        // Добавляем мигатель
        initialState[5, 5] = true;
        initialState[5, 6] = true;
        initialState[5, 7] = true;

        var board = new Board(10, 10, 1, initialState);
        var clusters = board.FindClusters();
        var classification = board.ClassifyClusters(clusters);

        Assert.Equal(1, classification["Block"]);
        Assert.Equal(1, classification["Blinker"]);
        Assert.Equal(0, classification["Glider"]);
        Assert.Equal(0, classification["Other"]);
    }
}
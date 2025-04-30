using Xunit;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace cli_life.Tests
{
    public class GameConfigTests
    {
        [Fact]
        public void GameConfig_Initialization_Test()
        {
            // Arrange & Act
            var config = new GameConfig(100, 50, 2, 0.3);

            // Assert
            Assert.Equal(100, config.Width);
            Assert.Equal(50, config.Height);
            Assert.Equal(2, config.CellSize);
            Assert.Equal(0.3, config.LiveDensity);
        }
    }

    public class FigureTests
    {
        [Fact]
        public void Figure_Initialization_Test()
        {
            // Arrange
            var cells = new int[][] { new int[] { 1, 0 }, new int[] { 0, 1 } };

            // Act
            var figure = new Figure
            {
                Name = "Test",
                Width = 2,
                Height = 2,
                Cells = cells
            };

            // Assert
            Assert.Equal("Test", figure.Name);
            Assert.Equal(2, figure.Width);
            Assert.Equal(2, figure.Height);
            Assert.Equal(cells, figure.Cells);
        }
    }

    public class BoardTests
    {
        private const int BoardSize = 50;

        [Fact]
        public void Board_Initialization_Test()
        {
            // Arrange & Act
            var board = new Board(BoardSize, BoardSize, 1, 0.3);

            // Assert
            Assert.Equal(BoardSize, board.Columns);
            Assert.Equal(BoardSize, board.Rows);
            Assert.Equal(BoardSize, board.Width);
            Assert.Equal(BoardSize, board.Height);
        }

        
        [Fact]
        public void Board_CountClusters_Test()
        {
            // Arrange
            var initialState = new bool[BoardSize, BoardSize];
            // Размещаем кластеры в центре доски
            initialState[BoardSize / 2, BoardSize / 2] = true;
            initialState[BoardSize / 2, BoardSize / 2 + 1] = true;
            initialState[BoardSize / 2 + 10, BoardSize / 2 + 10] = true; // Отдельный кластер
            var board = new Board(BoardSize, BoardSize, 1, initialState);

            // Act
            var clusters = board.CountClusters();

            // Assert
            Assert.Equal(2, clusters);
        }

        [Fact]
        public void TestForLake()
        {
            var gliderCells = new bool[BoardSize, BoardSize];
            gliderCells[3, 3] = true;
            gliderCells[3, 4] = true;
            gliderCells[4, 2] = true;
            gliderCells[5, 2] = true;
            gliderCells[6, 3] = true;
            gliderCells[6, 4] = true;
            gliderCells[6, 4] = true;
            gliderCells[4, 5] = true;
            gliderCells[5, 5] = true;

            var board = new Board(BoardSize, BoardSize, 1, gliderCells);
            
            for (int i = 0; i < 10; i++)
            {
                board.Advance();
            }

            Assert.True(board.Cells[3, 3].IsAlive);
            Assert.True(board.Cells[3, 4].IsAlive);
            Assert.True(board.Cells[4, 2].IsAlive);
            Assert.True(board.Cells[5, 2].IsAlive);
            Assert.True(board.Cells[6, 3].IsAlive);
            Assert.True(board.Cells[6, 4].IsAlive);
            Assert.True(board.Cells[4, 5].IsAlive);
            Assert.True(board.Cells[5, 5].IsAlive);
        }

        [Fact]
        public void TestForGlider()
        {
            // Arrange - создаем глайдер в центре доски
            var gliderCells = new bool[BoardSize, BoardSize];
            int centerX = BoardSize / 2;
            int centerY = BoardSize / 2;

            // Глайдер (форма)
            gliderCells[centerX, centerY] = true;
            gliderCells[centerX + 1, centerY] = true;
            gliderCells[centerX + 2, centerY] = true;
            gliderCells[centerX + 2, centerY - 1] = true;
            gliderCells[centerX + 1, centerY - 2] = true;

            var board = new Board(BoardSize, BoardSize, 1, gliderCells);

            // Act - делаем 4 шага (глайдер должен вернуться к исходной форме, но сместиться)
            for (int i = 0; i < 4; i++)
            {
                board.Advance();
            }

            // Assert - проверяем, что глайдер сохранился (но сместился)
            Assert.True(board.Cells[centerX + 1, centerY + 1].IsAlive);
            Assert.True(board.Cells[centerX + 2, centerY + 1].IsAlive);
            Assert.True(board.Cells[centerX + 3, centerY + 1].IsAlive);
            Assert.True(board.Cells[centerX + 3, centerY].IsAlive);
            Assert.True(board.Cells[centerX + 2, centerY - 1].IsAlive);
        }

        [Fact]
        public void TestGliderCollisionWithBlock()
        {
            // Arrange - создаем глайдер и блок на его пути
            var cells = new bool[BoardSize, BoardSize];
            cells[5, 8] = true;
            cells[6, 8] = true;
            cells[5, 9] = true;
            cells[6, 9] = true;
            cells[7, 6] = true;
            cells[8, 6] = true;
            cells[9, 6] = true;
            cells[9, 7] = true;
            cells[8, 9] = true;

            var board = new Board(BoardSize, BoardSize, 1, cells);

            // Act - делаем 1 шаг
            board.Advance();

            // Assert - проверяем столкновение
            Assert.True(board.Cells[8, 5].IsAlive);
            Assert.True(board.Cells[8, 6].IsAlive);
            Assert.True(board.Cells[9, 6].IsAlive);
            Assert.True(board.Cells[9, 7].IsAlive);
            Assert.True(board.Cells[6, 7].IsAlive);
            Assert.True(board.Cells[6, 8].IsAlive);
            Assert.True(board.Cells[7, 8].IsAlive);
            Assert.True(board.Cells[5, 8].IsAlive);
            Assert.True(board.Cells[5, 9].IsAlive);
            Assert.True(board.Cells[6, 9].IsAlive);
            Assert.True(board.Cells[7, 9].IsAlive);
        }

        
        [Fact]
        public void Board_SaveAndLoadState_Test()
        {
            // Arrange
            var initialState = new bool[BoardSize, BoardSize];
            initialState[BoardSize / 2, BoardSize / 2] = true;
            var board = new Board(BoardSize, BoardSize, 1, initialState);
            var tempFile = Path.GetTempFileName();

            try
            {
                // Act
                board.SaveState(tempFile);
                var loadedBoard = Board.LoadFromTextFile(tempFile);

                // Assert
                Assert.Equal(board.Columns, loadedBoard.Columns);
                Assert.Equal(board.Rows, loadedBoard.Rows);
                Assert.True(loadedBoard.Cells[BoardSize / 2, BoardSize / 2].IsAlive);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void Board_IsStable_Test()
        {
            // Arrange
            var board = new Board(BoardSize, BoardSize, 1);
            var counts = new int[] { 10, 10, 10, 10, 10 };

            // Act
            var isStable = board.IsStable(counts, 5);

            // Assert
            Assert.True(isStable);
        }

        [Fact]
        public void Board_ClassifyClusters_Test()
        {
            // Arrange
            var initialState = new bool[BoardSize, BoardSize];
            // Создаем квадрат в центре доски
            int centerX = BoardSize / 2;
            int centerY = BoardSize / 2;
            initialState[centerX, centerY] = true;
            initialState[centerX, centerY + 1] = true;
            initialState[centerX + 1, centerY] = true;
            initialState[centerX + 1, centerY + 1] = true;
            var board = new Board(BoardSize, BoardSize, 1, initialState);

            // Act
            var classification = board.ClassifyClusters();

            // Assert
            Assert.Contains("Square", classification.Keys);
            Assert.Equal(1, classification["Square"]);
        }
        [Fact]
        public void Board_ClassifyLake_Test()
        {
            var gliderCells = new bool[BoardSize, BoardSize];
            gliderCells[3, 3] = true;
            gliderCells[3, 4] = true;
            gliderCells[4, 2] = true;
            gliderCells[5, 2] = true;
            gliderCells[6, 3] = true;
            gliderCells[6, 4] = true;
            gliderCells[4, 5] = true;
            gliderCells[5, 5] = true;

            var board = new Board(BoardSize, BoardSize, 1, gliderCells);
            // Act
            var classification = board.ClassifyClusters();

            // Assert
            Assert.Contains("Lake", classification.Keys);
            Assert.Equal(1, classification["Lake"]);
        }

        [Fact]
        public void Board_ConnectNeighbors_Toroidal_Test()
        {
            // Arrange
            var board = new Board(BoardSize, BoardSize, 1);

            // Act
            var cornerCell = board.Cells[0, 0];
            var rightCell = board.Cells[1, 0];
            var bottomCell = board.Cells[0, 1];
            var farRightCell = board.Cells[BoardSize - 1, 0];
            var farBottomCell = board.Cells[0, BoardSize - 1];

            // Assert
            Assert.Contains(rightCell, cornerCell.neighbors);
            Assert.Contains(bottomCell, cornerCell.neighbors);
            Assert.Contains(farRightCell, cornerCell.neighbors); // Toroidal right
            Assert.Contains(farBottomCell, cornerCell.neighbors); // Toroidal bottom
        }

        [Fact]
        public void Board_Advance_BlinkerPattern_Test()
        {
            // Arrange - создаем мигалку в центре доски (вертикальную)
            var initialState = new bool[BoardSize, BoardSize];
            int centerX = BoardSize / 2;
            int centerY = BoardSize / 2;
            initialState[centerX, centerY - 1] = true;
            initialState[centerX, centerY] = true;
            initialState[centerX, centerY + 1] = true;
            var board = new Board(BoardSize, BoardSize, 1, initialState);

            // Act - после одного поколения она должна стать горизонтальной
            board.Advance();

            // Assert
            Assert.True(board.Cells[centerX - 1, centerY].IsAlive);
            Assert.True(board.Cells[centerX, centerY].IsAlive);
            Assert.True(board.Cells[centerX + 1, centerY].IsAlive);
        }

        [Fact]
        public void Board_MoveCoordinatesIn00_Test()
        {
            // Arrange
            var board = new Board(BoardSize, BoardSize, 1);
            var cells = new List<(int, int)> { (BoardSize / 2, BoardSize / 2), (BoardSize / 2, BoardSize / 2 + 1), (BoardSize / 2 + 1, BoardSize / 2) };

            // Act
            var normalized = board.MoveCoordinatesIn00(cells);

            // Assert
            Assert.Contains((0, 0), normalized);
            Assert.Contains((0, 1), normalized);
            Assert.Contains((1, 0), normalized);
        }

        [Fact]
        public void Board_GetNeighbors_Test()
        {
            // Arrange
            var board = new Board(BoardSize, BoardSize, 1);

            // Act
            var neighbors = board.GetNeighbors(BoardSize / 2, BoardSize / 2);

            // Assert
            Assert.Equal(8, neighbors.Count);
            Assert.Contains((BoardSize / 2 - 1, BoardSize / 2 - 1), neighbors);
            Assert.Contains((BoardSize / 2, BoardSize / 2 - 1), neighbors);
            Assert.Contains((BoardSize / 2 + 1, BoardSize / 2 - 1), neighbors);
            Assert.Contains((BoardSize / 2 - 1, BoardSize / 2), neighbors);
            Assert.Contains((BoardSize / 2 + 1, BoardSize / 2), neighbors);
            Assert.Contains((BoardSize / 2 - 1, BoardSize / 2 + 1), neighbors);
            Assert.Contains((BoardSize / 2, BoardSize / 2 + 1), neighbors);
            Assert.Contains((BoardSize / 2 + 1, BoardSize / 2 + 1), neighbors);
        }

        [Fact]
        public void Board_Randomize_Test()
        {
            // Arrange
            var board = new Board(BoardSize, BoardSize, 1, 0.5);

            // Act
            int liveCount = board.CountLiveCells();

            // Assert - поскольку случайно, проверяем только приблизительное значение
            Assert.InRange(liveCount, BoardSize * BoardSize * 0.4, BoardSize * BoardSize * 0.6); // 50% от BoardSize² ±10%
        }
    }
}
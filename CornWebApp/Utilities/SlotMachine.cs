using CornWebApp.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CornWebApp.Utilities
{
    public class SlotMachine
    {

        public enum BoxValue
        {
            None,
            Corn,
            Popcorn,
            Unicorn,
        }

        public int Size { get; private set; }
        public int RevealProgress { get; set; }
        public long Bet { get; private set; }

        private readonly Random random;
        private readonly BoxValue[][] grid;
        

        public SlotMachine(int size, long bet, Random random)
        {
            this.Size = size;
            this.random = random;
            this.Bet = bet;
            grid = new BoxValue[size][];
            InitializeGrid();
        }

        private void InitializeGrid()
        {
            for (int y = 0; y < Size; y++)
            {
                grid[y] = new BoxValue[Size];
                for (int x = 0; x < Size; x++)
                {
                    switch (random.Next(0, 5))
                    {
                        case 0:
                            grid[y][x] = BoxValue.Corn;
                            break;
                        case 1:
                        case 2:
                            grid[y][x] = BoxValue.Unicorn;
                            break;
                        case 3:
                        case 4:
                            grid[y][x] = BoxValue.Popcorn;
                            break;
                    }
                }

            }
        }

        public string GetStringRepresentation()
        {
            var sb = new StringBuilder();
            for (int row = 0; row < Size; row++)
            {
                for (int col = 0; col < Size; col++)
                {
                    sb.Append(grid[row][col] switch
                    {
                        BoxValue.Corn => "C",
                        BoxValue.Popcorn => "P",
                        BoxValue.Unicorn => "U",
                        _ => "X",
                    });
                }
                sb.Append('\n');
            }
            return sb.ToString();
        }

        public Dictionary<BoxValue, int> GetMatches()
        {
            Dictionary<BoxValue, int> matches = new()
            {
                [BoxValue.Corn] = 0,
                [BoxValue.Popcorn] = 0,
                [BoxValue.Unicorn] = 0
            };

            // check rows
            foreach (var row in grid)
            {
                if (row.All(box => box == row[0]))
                {
                    matches[row[0]]++;
                }
            }

            // check columns
            for (int col = 0; col < Size; col++)
            {
                if (grid.All(row => row[col] == grid[0][col]))
                {
                    matches[grid[0][col]]++;
                }
            }

            // check diagonals
            bool backDiagMatch = true;
            bool forwardDiagMatch = true;
            for (int pos = 0; pos < Size; pos++)
            {
                if (grid[pos][pos] != grid[0][0])
                    backDiagMatch = false;
                if (grid[pos][Size - pos - 1] != grid[0][Size - 1])
                    forwardDiagMatch = false;
            }
            if (backDiagMatch)
                matches[grid[0][0]]++;
            if (forwardDiagMatch)
                matches[grid[0][Size - 1]]++;

            return matches;
        }

        public long GetWinnings()
        {
            double multiplier = 0.0;
            var matches = GetMatches();

            multiplier += matches[BoxValue.Corn] * 3.0;
            multiplier += matches[BoxValue.Unicorn];
            multiplier += matches[BoxValue.Popcorn];

            multiplier = 0.2 + multiplier * 0.9;

            return (long)Math.Round(multiplier * Bet);
        }

    }
}

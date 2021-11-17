// -----------------------------------------------------------------------
// <copyright file="ScreenSaver.cs" company="SyukoTech">
// Copyright (c) SyukoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MazeScreenSaver
{
    using MazeGenerator.Generator;
    using MazeGenerator.Type;
    using MazeGenerator.Type.Base;
    using MazeGenerator.Type.MazeObject;
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Linq;
    using System.Threading;
    using System.Windows.Forms;

    public partial class ScreenSaver : Form
    {
        private readonly IMazeGenerator mazeGenerator;

        private readonly CancellationTokenSource tokenSource = new();

        private readonly Color wayColor = Color.White;

        private readonly Color wayToExitColor = Color.Gold;

        private bool drawWayToExit;

        public ScreenSaver()
        {
            this.InitializeComponent();

            this.mazeGenerator = new DefaultMaze(100, 200) { Configuration = new Configuration(5, 5) };

            Maze maze = this.mazeGenerator.InitMaze();
            maze.MazeCellUpdated += this.Maze_MazeCellUpdated;

            _ = this.mazeGenerator.Generate(this.tokenSource.Token).ContinueWith(_ => this.DrawWayToExit());
        }

        private static void DrawMazeCell(Graphics graphics, Color color, MazeCell mazeCell)
        {
            (EDirection directions, (int x, int y)) = mazeCell;

            graphics.FillRectangle(new SolidBrush(color), (x * 2) + 1, (y * 2) + 1, 1, 1);

            if (directions.HasFlag(EDirection.Down))
            {
                graphics.FillRectangle(new SolidBrush(color), (x * 2) + 1, (y * 2) + 2, 1, 1);
            }

            if (directions.HasFlag(EDirection.Left))
            {
                graphics.FillRectangle(new SolidBrush(color), x * 2, (y * 2) + 1, 1, 1);
            }

            if (directions.HasFlag(EDirection.Right))
            {
                graphics.FillRectangle(new SolidBrush(color), (x * 2) + 2, (y * 2) + 1, 1, 1);
            }

            if (directions.HasFlag(EDirection.Top))
            {
                graphics.FillRectangle(new SolidBrush(color), (x * 2) + 1, y * 2, 1, 1);
            }
        }

        private void DrawWayToExit()
        {
            if (this.mazeGenerator.WayToExit == null || this.mazeGenerator.WayToExit.Count == 0)
            {
#if DEBUG
                Console.Out.WriteLine("No way to exit to be drawn");
#endif
                return;
            }

            this.drawWayToExit = true;
            this.Invalidate();
        }

        private void Maze_MazeCellUpdated(object sender, MazeCellUpdatedEventArgs args) => this.Invalidate();

        private void ScreenSaver_KeyPress(object sender, KeyPressEventArgs e)
        {
            this.tokenSource?.Cancel();

            Application.Exit();
        }

        private void ScreenSaver_Paint(object sender, PaintEventArgs e)
        {
            if (this.InvokeRequired)
            {
                new PaintEventHandler(this.ScreenSaver_Paint).Invoke(sender, e);
                return;
            }

            var srcRect = new RectangleF(this.Padding.Left, this.Padding.Top, this.Width - this.Padding.Left - this.Padding.Right, this.Height - this.Padding.Top - this.Padding.Bottom);
            var destRect = new RectangleF(0.0F, 0.0F, (this.mazeGenerator.Width * 2) + 1, (this.mazeGenerator.Height * 2) + 1);

            GraphicsContainer containerState = e.Graphics.BeginContainer(srcRect, destRect, GraphicsUnit.Pixel);

            e.Graphics.FillRectangle(new SolidBrush(this.wayColor), this.mazeGenerator.Maze.Entry.X * 2, (this.mazeGenerator.Maze.Entry.Y * 2) + 1, 1, 1);
            e.Graphics.FillRectangle(new SolidBrush(this.wayColor), (this.mazeGenerator.Maze.Exit.X * 2) + 2, (this.mazeGenerator.Maze.Exit.Y * 2) + 1, 1, 1);

            lock (this.mazeGenerator.Maze)
            {
                IEnumerable<MazeCell> mazeCells = this.mazeGenerator.Maze.Where(c => c.Directions != EDirection.None);
                foreach (MazeCell mazeCell in mazeCells)
                {
                    ScreenSaver.DrawMazeCell(e.Graphics, this.wayColor, mazeCell);
                }
            }

            if (this.drawWayToExit)
            {
                e.Graphics.FillRectangle(new SolidBrush(this.wayToExitColor), this.mazeGenerator.Maze.Entry.X * 2, (this.mazeGenerator.Maze.Entry.Y * 2) + 1, 1, 1);

                Coordinates coordinates = this.mazeGenerator.Maze.Entry;

                foreach (EDirection direction in this.mazeGenerator.WayToExit)
                {
                    var mazeCell = new MazeCell(direction, coordinates);

                    ScreenSaver.DrawMazeCell(e.Graphics, this.wayToExitColor, mazeCell);

                    coordinates = direction switch
                    {
                        EDirection.Down => (coordinates.X, coordinates.Y + 1),
                        EDirection.Left => (coordinates.X - 1, coordinates.Y),
                        EDirection.Right => (coordinates.X + 1, coordinates.Y),
                        EDirection.Top => (coordinates.X, coordinates.Y - 1),
                        _ => coordinates,
                    };
                }

                e.Graphics.FillRectangle(new SolidBrush(this.wayToExitColor), (this.mazeGenerator.Maze.Exit.X * 2) + 1, (this.mazeGenerator.Maze.Exit.Y * 2) + 1, 2, 1);
            }

            e.Graphics.EndContainer(containerState);
        }

        private void ScreenSaver_Resize(object sender, EventArgs e) => this.Invalidate();
    }
}
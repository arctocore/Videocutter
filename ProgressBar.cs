using System;
using System.Text;
using System.Threading;

namespace ArcToCore.Core.HandballBoard.VideoCutter
{
    /// <summary>
    /// An ASCII progress bar
    /// </summary>
    public class ProgressBar : IDisposable, IProgress<double>
    {
        /// <summary>
        /// Defines the blockCount.
        /// </summary>
        private const int blockCount = 10;

        /// <summary>
        /// Defines the animationInterval.
        /// </summary>
        private readonly TimeSpan animationInterval = TimeSpan.FromSeconds(1.0 / 8);

        /// <summary>
        /// Defines the animation.
        /// </summary>
        private const string animation = @"|/-\";

        /// <summary>
        /// Defines the timer.
        /// </summary>
        private readonly Timer timer;

        /// <summary>
        /// Defines the currentProgress.
        /// </summary>
        private double currentProgress = 0;

        /// <summary>
        /// Defines the currentText.
        /// </summary>
        private string currentText = string.Empty;

        /// <summary>
        /// Defines the disposed.
        /// </summary>
        private bool disposed = false;

        /// <summary>
        /// Defines the animationIndex.
        /// </summary>
        private int animationIndex = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressBar"/> class.
        /// </summary>
        public ProgressBar()
        {
            timer = new Timer(TimerHandler);

            // A progress bar is only for temporary display in a console window.
            // If the console output is redirected to a file, draw nothing.
            // Otherwise, we'll end up with a lot of garbage in the target file.
            if (!Console.IsOutputRedirected)
            {
                ResetTimer();
            }
        }

        /// <summary>
        /// The Report.
        /// </summary>
        /// <param name="value">The value<see cref="double"/>.</param>
        public void Report(double value)
        {
            // Make sure value is in [0..1] range
            value = Math.Max(0, Math.Min(1, value));
            Interlocked.Exchange(ref currentProgress, value);
        }

        /// <summary>
        /// The TimerHandler.
        /// </summary>
        /// <param name="state">The state<see cref="object"/>.</param>
        private void TimerHandler(object state)
        {
            lock (timer)
            {
                if (disposed) return;

                int progressBlockCount = (int)(currentProgress * blockCount);
                int percent = (int)(currentProgress * 100);
                string text = string.Format("[{0}{1}] {2,3}% {3}",
                    new string('#', progressBlockCount), new string('-', blockCount - progressBlockCount),
                    percent,
                    animation[animationIndex++ % animation.Length]);
                UpdateText(text);

                ResetTimer();
            }
        }

        /// <summary>
        /// The UpdateText.
        /// </summary>
        /// <param name="text">The text<see cref="string"/>.</param>
        private void UpdateText(string text)
        {
            // Get length of common portion
            int commonPrefixLength = 0;
            int commonLength = Math.Min(currentText.Length, text.Length);
            while (commonPrefixLength < commonLength && text[commonPrefixLength] == currentText[commonPrefixLength])
            {
                commonPrefixLength++;
            }

            // Backtrack to the first differing character
            StringBuilder outputBuilder = new StringBuilder();
            outputBuilder.Append('\b', currentText.Length - commonPrefixLength);

            // Output new suffix
            outputBuilder.Append(text.Substring(commonPrefixLength));

            // If the new text is shorter than the old one: delete overlapping characters
            int overlapCount = currentText.Length - text.Length;
            if (overlapCount > 0)
            {
                outputBuilder.Append(' ', overlapCount);
                outputBuilder.Append('\b', overlapCount);
            }

            Console.Write(outputBuilder);
            currentText = text;
        }

        /// <summary>
        /// The ResetTimer.
        /// </summary>
        private void ResetTimer()
        {
            timer.Change(animationInterval, TimeSpan.FromMilliseconds(-1));
        }

        /// <summary>
        /// The Dispose.
        /// </summary>
        public void Dispose()
        {
            lock (timer)
            {
                disposed = true;
                UpdateText(string.Empty);
            }
        }
    }
}
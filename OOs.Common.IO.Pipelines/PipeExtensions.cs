using System.IO.Pipelines;

#pragma warning disable CA1034 // Nested types should not be visible

namespace OOs.IO.Pipelines;

public static class PipeExtensions
{
    extension(Pipe pipe)
    {
        public void Deconstruct(out PipeReader reader, out PipeWriter writer)
        {
            ArgumentNullException.ThrowIfNull(pipe);

            reader = pipe.Reader;
            writer = pipe.Writer;
        }

        /// <summary>
        /// Completes the pipe, signaling both the reader and writer that no more data will be available.
        /// </summary>
        public void Complete()
        {
            ArgumentNullException.ThrowIfNull(pipe);

            pipe.Reader.Complete();
            pipe.Writer.Complete();
        }

        /// <summary>
        /// Completes the pipe asynchronously, signaling both the reader and writer that no more data will be available.
        /// </summary>
        /// <returns>A value task representing the asynchronous operation.</returns>
        public ValueTask CompleteAsync()
        {
            ArgumentNullException.ThrowIfNull(pipe);

            var readerCompleted = pipe.Reader.CompleteAsync();
            var writerCompleted = pipe.Writer.CompleteAsync();

            return readerCompleted.IsCompletedSuccessfully && writerCompleted.IsCompletedSuccessfully
                ? ValueTask.CompletedTask
                : CompleteAsync(readerCompleted, writerCompleted);

            static async ValueTask CompleteAsync(ValueTask readerCompleted, ValueTask writerCompleted)
            {
                await readerCompleted.ConfigureAwait(false);
                await writerCompleted.ConfigureAwait(false);
            }
        }
    }
}
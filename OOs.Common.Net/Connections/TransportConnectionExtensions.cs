namespace OOs.Net.Connections;

public static class TransportConnectionExtensions
{
#pragma warning disable CA1034 // Nested types should not be visible
    extension(TransportConnection connection)
#pragma warning restore CA1034 // Nested types should not be visible
    {
        /// <summary>
        /// Attempts to close the transport connection gracefully (processing 4-way teardown logic).
        /// </summary>
        /// <param name="cancellationToken">
        /// A cancellation token used to propagate notification that the operation should be canceled.
        /// </param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Use this method thoroughly and mindfully, as it may not complete due to
        /// other party doesn't shutdown properly (missing respective FIN from other side e.g.).
        /// Do not await on the returned task unconditionally. Use reasonable timeout or cancellation 
        /// logic in order to avoid indefinite waiting and then ubruptly terminate connection via Abort() e.g.
        /// </remarks>
        public async Task CloseAsync(CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(connection);

            await connection.CloseOutputAsync().ConfigureAwait(false);

            var input = connection.Input;

            try
            {
                while (true)
                {
                    var result = await input.ReadAsync(cancellationToken).ConfigureAwait(false);

                    if (result.IsCanceled)
                    {
                        break;
                    }

                    input.AdvanceTo(consumed: result.Buffer.End);

                    if (result.IsCompleted)
                    {
                        break;
                    }
                }
            }
            finally
            {
                await input.CompleteAsync().ConfigureAwait(false);
            }
        }
    }
}
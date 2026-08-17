namespace BuildingBlocks.Idempotency.Internal;

/// <summary>
///     包装响应流：在转发到目标流的同时，于内存中缓冲不超过上限的响应体以便重放。
/// </summary>
/// <remarks>
///     超出上限后进入透传模式并丢弃缓冲，使响应仍可正常返回但不可重放。
/// </remarks>
internal sealed class BoundedResponseBufferStream(Stream destination, long maxCaptureBytes) : Stream
{
    private readonly MemoryStream _buffer = new();
    private bool _captureExceeded;
    private bool _passThrough;

    /// <summary>
    ///     获取响应体是否仍在可重放上限内（未被截断）。
    /// </summary>
    public bool Replayable => !_captureExceeded;

    /// <summary>
    ///     返回缓冲的响应体；不可重放时返回空数组。
    /// </summary>
    /// <returns>捕获的响应体字节。</returns>
    public byte[] GetCapturedBody()
    {
        return Replayable ? _buffer.ToArray() : [];
    }

    /// <summary>
    ///     将缓冲内容刷写到目标流并刷新。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        if (!_passThrough)
        {
            _buffer.Position = 0;
            await _buffer.CopyToAsync(destination, cancellationToken);
            _passThrough = true;
        }

        await destination.FlushAsync(cancellationToken);
    }

    /// <summary>
    ///     切换到透传模式（若尚未）并刷新目标流。
    /// </summary>
    public override void Flush()
    {
        StartPassThrough();
        destination.Flush();
    }

    /// <summary>
    ///     异步切换到透传模式（若尚未）并刷新目标流。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    public override async Task FlushAsync(CancellationToken cancellationToken)
    {
        await StartPassThroughAsync(cancellationToken);
        await destination.FlushAsync(cancellationToken);
    }

    /// <summary>
    ///     将托管数组片段写入流。
    /// </summary>
    /// <param name="buffer">源缓冲区。</param>
    /// <param name="offset">起始偏移。</param>
    /// <param name="count">写入字节数。</param>
    public override void Write(byte[] buffer, int offset, int count)
    {
        Write(buffer.AsSpan(offset, count));
    }

    /// <summary>
    ///     写入数据：可捕获时缓冲，否则透传并标记不可重放。
    /// </summary>
    /// <param name="buffer">待写入的数据。</param>
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        if (!_passThrough && CanCapture(buffer.Length))
        {
            _buffer.Write(buffer);
            return;
        }

        if (!_passThrough)
            StartOverflowPassThrough();
        else
            CaptureWhilePassingThrough(buffer);

        destination.Write(buffer);
    }

    /// <summary>
    ///     将托管数组片段异步写入流。
    /// </summary>
    /// <param name="buffer">源缓冲区。</param>
    /// <param name="offset">起始偏移。</param>
    /// <param name="count">写入字节数。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>表示异步写入的任务。</returns>
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }

    /// <summary>
    ///     异步写入数据：可捕获时缓冲，否则透传并标记不可重放。
    /// </summary>
    /// <param name="buffer">待写入的数据。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>表示异步写入的值任务。</returns>
    public override async ValueTask WriteAsync(
        ReadOnlyMemory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        if (!_passThrough && CanCapture(buffer.Length))
        {
            await _buffer.WriteAsync(buffer, cancellationToken);
            return;
        }

        if (!_passThrough)
            await StartOverflowPassThroughAsync(cancellationToken);
        else
            CaptureWhilePassingThrough(buffer.Span);

        await destination.WriteAsync(buffer, cancellationToken);
    }

    /// <summary>
    ///     判断本次写入是否仍可捕获而不超过上限。
    /// </summary>
    /// <param name="count">本次写入字节数。</param>
    /// <returns>可捕获返回 <see langword="true"/>。</returns>
    private bool CanCapture(int count)
    {
        return !_captureExceeded && _buffer.Length + count <= maxCaptureBytes;
    }

    /// <summary>
    ///     在已透传状态下尝试补捕获剩余内容，超限则清空缓冲。
    /// </summary>
    /// <param name="buffer">待写入的数据。</param>
    private void CaptureWhilePassingThrough(ReadOnlySpan<byte> buffer)
    {
        if (_captureExceeded)
            return;

        if (CanCapture(buffer.Length))
        {
            _buffer.Write(buffer);
            return;
        }

        _captureExceeded = true;
        _buffer.SetLength(0);
    }

    /// <summary>
    ///     将已缓冲内容一次性写入目标流并进入透传模式（幂等）。
    /// </summary>
    private void StartPassThrough()
    {
        if (_passThrough)
            return;

        _buffer.WriteTo(destination);
        _passThrough = true;
    }

    /// <summary>
    ///     异步将已缓冲内容写入目标流并进入透传模式（幂等）。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    private async Task StartPassThroughAsync(CancellationToken cancellationToken)
    {
        if (_passThrough)
            return;

        _buffer.Position = 0;
        await _buffer.CopyToAsync(destination, cancellationToken);
        _buffer.Position = _buffer.Length;
        _passThrough = true;
    }

    /// <summary>
    ///     在发生溢出时丢弃缓冲并切换为纯透传。
    /// </summary>
    private void StartOverflowPassThrough()
    {
        _captureExceeded = true;
        _buffer.WriteTo(destination);
        _buffer.SetLength(0);
        _passThrough = true;
    }

    /// <summary>
    ///     异步在溢出时丢弃缓冲并切换为纯透传。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    private async Task StartOverflowPassThroughAsync(CancellationToken cancellationToken)
    {
        _captureExceeded = true;
        _buffer.Position = 0;
        await _buffer.CopyToAsync(destination, cancellationToken);
        _buffer.SetLength(0);
        _passThrough = true;
    }

    /// <summary>
    ///     释放底层缓冲流。
    /// </summary>
    /// <param name="disposing">是否由托管代码释放。</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _buffer.Dispose();

        base.Dispose(disposing);
    }

    /// <summary>该流不可读。</summary>
    public override bool CanRead => false;
    /// <summary>该流不可定位。</summary>
    public override bool CanSeek => false;
    /// <summary>该流可写。</summary>
    public override bool CanWrite => true;
    /// <summary>不支持获取长度。</summary>
    public override long Length => throw new NotSupportedException();

    /// <summary>不支持获取或设置位置。</summary>
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    /// <summary>该流不可读，调用即抛异常。</summary>
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    /// <summary>不支持定位。</summary>
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    /// <summary>不支持设置长度。</summary>
    public override void SetLength(long value) => throw new NotSupportedException();
}
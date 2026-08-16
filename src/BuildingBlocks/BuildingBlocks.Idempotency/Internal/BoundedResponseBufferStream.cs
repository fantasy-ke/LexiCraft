namespace BuildingBlocks.Idempotency.Internal;

internal sealed class BoundedResponseBufferStream(Stream destination, long maxCaptureBytes) : Stream
{
    private readonly MemoryStream _buffer = new();
    private bool _captureExceeded;
    private bool _passThrough;

    public bool Replayable => !_captureExceeded;

    public byte[] GetCapturedBody()
    {
        return Replayable ? _buffer.ToArray() : [];
    }

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

    public override void Flush()
    {
        StartPassThrough();
        destination.Flush();
    }

    public override async Task FlushAsync(CancellationToken cancellationToken)
    {
        await StartPassThroughAsync(cancellationToken);
        await destination.FlushAsync(cancellationToken);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        Write(buffer.AsSpan(offset, count));
    }

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

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }

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

    private bool CanCapture(int count)
    {
        return !_captureExceeded && _buffer.Length + count <= maxCaptureBytes;
    }

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

    private void StartPassThrough()
    {
        if (_passThrough)
            return;

        _buffer.WriteTo(destination);
        _passThrough = true;
    }

    private async Task StartPassThroughAsync(CancellationToken cancellationToken)
    {
        if (_passThrough)
            return;

        _buffer.Position = 0;
        await _buffer.CopyToAsync(destination, cancellationToken);
        _buffer.Position = _buffer.Length;
        _passThrough = true;
    }

    private void StartOverflowPassThrough()
    {
        _captureExceeded = true;
        _buffer.WriteTo(destination);
        _buffer.SetLength(0);
        _passThrough = true;
    }

    private async Task StartOverflowPassThroughAsync(CancellationToken cancellationToken)
    {
        _captureExceeded = true;
        _buffer.Position = 0;
        await _buffer.CopyToAsync(destination, cancellationToken);
        _buffer.SetLength(0);
        _passThrough = true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _buffer.Dispose();

        base.Dispose(disposing);
    }

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
}
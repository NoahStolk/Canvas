using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable S4136 // Method overloads should be grouped together

namespace Blazor.Extensions.Canvas;

[StructLayout(LayoutKind.Explicit)]
public struct InteropUniformMatrix4fv
{
    [FieldOffset(0)]
    public int LocationId;

    [FieldOffset(8)]
    public int MatrixId;
}

public abstract class RenderingContext : IDisposable
{
    private const string NAMESPACE_PREFIX = "BlazorExtensions";
    private const string GET_PROPERTY_ACTION = "getProperty";
    private const string CALL_METHOD_ACTION = "call";
    private const string CALL_BATCH_ACTION = "callBatch";
    private const string ADD_ACTION = "add";
    private const string REMOVE_ACTION = "remove";
    private readonly List<object[]> _batchedCallObjects = new List<object[]>();
    private readonly string _contextName;
    private readonly IJSRuntime _jsRuntime;
    protected readonly IJSInProcessRuntime jsIn;
    protected readonly IJSUnmarshalledRuntime jsUn;
    private readonly object _parameters;
    private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

    private bool _awaitingBatchedCall;
    private bool _batching;
    private bool _initialized;

    public ElementReference Canvas { get; }

    protected RenderingContext(BECanvasComponent reference, string contextName, object parameters = null)
    {
        Canvas = reference.CanvasReference;
        _contextName = contextName;
        _jsRuntime = reference.JSRuntime;
        jsIn = reference.JSInProcessRuntime;
        jsUn = reference.JSUnmarshalledRuntime;
        _parameters = parameters;
    }

    protected virtual Task ExtendedInitializeAsync() { return Task.CompletedTask; }

    protected virtual void ExtendedInitialize() { }

    internal async Task<RenderingContext> InitializeAsync()
    {
        await _semaphoreSlim.WaitAsync();
        if (!_initialized)
        {
            await _jsRuntime.InvokeAsync<object>($"{NAMESPACE_PREFIX}.{_contextName}.{ADD_ACTION}", Canvas, _parameters);
            await ExtendedInitializeAsync();
            _initialized = true;
        }
        _semaphoreSlim.Release();
        return this;
    }

    internal RenderingContext Initialize()
    {
        if (!_initialized)
        {
            jsIn.Invoke<object>($"{NAMESPACE_PREFIX}.{_contextName}.{ADD_ACTION}", Canvas, _parameters);
            ExtendedInitialize();
            _initialized = true;
        }
        return this;
    }

    #region Protected Methods

    public async Task BeginBatchAsync()
    {
        await _semaphoreSlim.WaitAsync();
        _batching = true;
        _semaphoreSlim.Release();
    }

    public async Task EndBatchAsync()
    {
        await _semaphoreSlim.WaitAsync();

        await BatchCallInnerAsync();
    }

    protected async Task BatchCallAsync(string name, bool isMethodCall, params object[] value)
    {
        await _semaphoreSlim.WaitAsync();

        var callObject = new object[value.Length + 2];
        callObject[0] = name;
        callObject[1] = isMethodCall;
        Array.Copy(value, 0, callObject, 2, value.Length);
        _batchedCallObjects.Add(callObject);

        if (_batching || _awaitingBatchedCall)
        {
            _semaphoreSlim.Release();
        }
        else
        {
            await BatchCallInnerAsync();
        }
    }

    protected async Task<T> GetPropertyAsync<T>(string property)
    {
        return await _jsRuntime.InvokeAsync<T>($"{NAMESPACE_PREFIX}.{_contextName}.{GET_PROPERTY_ACTION}", Canvas, property);
    }

    public PerfContext Context { get; } = new PerfContext();

    protected T CallMethod<T>(string method)
    {
        using PerfMeasure perfMeasure = new PerfMeasure(Context, method);
        return jsIn.Invoke<T>($"{NAMESPACE_PREFIX}.{_contextName}.{CALL_METHOD_ACTION}", Canvas, method);
    }

    protected async Task<T> CallMethodAsync<T>(string method)
    {
        return await _jsRuntime.InvokeAsync<T>($"{NAMESPACE_PREFIX}.{_contextName}.{CALL_METHOD_ACTION}", Canvas, method);
    }

    protected T CallMethod<T>(string method, params object[] value)
    {
        using PerfMeasure perfMeasure = new PerfMeasure(Context, method);
        return jsIn.Invoke<T>($"{NAMESPACE_PREFIX}.{_contextName}.{CALL_METHOD_ACTION}", Canvas, method, value);
    }
        
    protected async Task<T> CallMethodAsync<T>(string method, params object[] value)
    {
        return await _jsRuntime.InvokeAsync<T>($"{NAMESPACE_PREFIX}.{_contextName}.{CALL_METHOD_ACTION}", Canvas, method, value);
    }

    private async Task BatchCallInnerAsync()
    {
        _awaitingBatchedCall = true;
        var currentBatch = _batchedCallObjects.ToArray();
        _batchedCallObjects.Clear();
        _semaphoreSlim.Release();

        _ = await _jsRuntime.InvokeAsync<object>($"{NAMESPACE_PREFIX}.{_contextName}.{CALL_BATCH_ACTION}", Canvas, currentBatch);

        await _semaphoreSlim.WaitAsync();
        _awaitingBatchedCall = false;
        _batching = false;
        _semaphoreSlim.Release();
    }

    public void Dispose()
    {
        Task.Run(async () => await _jsRuntime.InvokeAsync<object>($"{NAMESPACE_PREFIX}.{_contextName}.{REMOVE_ACTION}", Canvas));
    }

    #endregion
}
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;

namespace Blazor.Extensions.Canvas;

public class BECanvasComponent : ComponentBase
{
    [Parameter]
    public long Height { get; set; }

    [Parameter]
    public long Width { get; set; }

    [Parameter]
    public Action<MouseEventArgs> OnClick { get; set; }

    [Parameter]
    public Action<MouseEventArgs> OnMouseDown { get; set; }

    [Parameter]
    public Action<MouseEventArgs> OnMouseMove { get; set; }

    [Parameter]
    public Action<MouseEventArgs> OnMouseOut { get; set; }

    [Parameter]
    public Action<MouseEventArgs> OnMouseOver { get; set; }

    [Parameter]
    public Action<MouseEventArgs> OnMouseUp { get; set; }

    [Parameter]
    public Action<MouseEventArgs> OnMouseWheel { get; set; }

    protected readonly string Id = Guid.NewGuid().ToString();
    protected ElementReference _canvasRef;

    internal ElementReference CanvasReference => _canvasRef;

    [Inject]
    internal IJSRuntime JSRuntime { get; set; }

    [Inject]
    internal IJSInProcessRuntime JSInProcessRuntime { get; set; }

    [Inject]
    internal IJSUnmarshalledRuntime JSUnmarshalledRuntime { get; set; }
}
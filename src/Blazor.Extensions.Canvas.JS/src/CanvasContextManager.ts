declare global {
    interface Window {
        Blazor: any;
    }
}

// declare const window: any;

export class ContextManager {
    private gl: any;
  private readonly contexts = new Map<string, any>();
  private readonly webGLObject = new Array<any>();
  private readonly contextName: string;
  private webGLContext = false;
  private readonly prototypes: any;
  private readonly webGLTypes = [
    WebGLBuffer, WebGLShader, WebGLProgram, WebGLFramebuffer, WebGLRenderbuffer, WebGLTexture, WebGLUniformLocation
  ];

  public constructor(contextName: string) {
    this.contextName = contextName;
    if (contextName === "2d")
      this.prototypes = CanvasRenderingContext2D.prototype;
    else if (contextName === "webgl" || contextName === "experimental-webgl") {
      this.prototypes = WebGLRenderingContext.prototype;
      this.webGLContext = true;
    } else
      throw new Error(`Invalid context name: ${contextName}`);
  }

  public add = (canvas: HTMLCanvasElement, parameters: any) => {
    if (!canvas) throw new Error('Invalid canvas.');
    if (this.contexts.get(canvas.id)) return;

    var context;
    if (parameters)
      context = canvas.getContext(this.contextName, parameters);
    else
      context = canvas.getContext(this.contextName);

    if (!context) throw new Error('Invalid context.');

    this.contexts.set(canvas.id, context);
  }

  public remove = (canvas: HTMLCanvasElement) => {
    this.contexts.delete(canvas.id);
  }

  public setProperty = (canvas: HTMLCanvasElement, property: string, value: any) => {
    const context = this.getContext(canvas);
    this.setPropertyWithContext(context, property, value);
  }

  public getProperty = (canvas: HTMLCanvasElement, property: string) => {
    const context = this.getContext(canvas);
    return this.serialize(context[property]);
  }

  public call = (canvas: HTMLCanvasElement, method: string, args: any) => {
      const context = this.getContext(canvas);
      return this.callWithContext(context, method, args);
  }


  public pinGLEx = (canvas: HTMLCanvasElement) => {
      this.gl = this.getContext(canvas);
  }

    public createPinEx = (canvas: HTMLCanvasElement) => {
        const id = this.webGLObject.length;
        this.webGLObject.push(this.getContext(canvas));
        return { webGLType: 'WebGLPin', id: id };
    }

    public usePinEx = (pinId: any) => {
        this.gl = this.webGLObject[pinId];
    }

    public createUniformMatrixEx = (floats: any) => {
        const id = this.webGLObject.length;
        this.webGLObject.push(floats);
        return { webGLType: 'WebGLUniformMatrix', id: id };
    }

    public renderUniformMatrixIEx = (args: any) => {
      const locationId = window.Blazor.platform.readInt32Field(args, 0);
      const matrixId = window.Blazor.platform.readInt32Field(args, 8);
      this.gl.uniformMatrix4fv(this.webGLObject[locationId], false, this.webGLObject[matrixId]);
    }

    public renderUniformMatrixEx = (locationId: any, matrixId: any) => {
      this.gl.uniformMatrix4fv(this.webGLObject[locationId], false, this.webGLObject[matrixId]);
    }

    // conversion to js float 32 array from:
    // https://stackoverflow.com/questions/63902497/blazor-wasm-invoke-javascript-pass-large-array-is-very-slow
    //
    public uniformMatrix4fvEx = (locationId: any, floats: any) => {
        var m = floats + 12;
        var r = Module.HEAP32[m >> 2];
        var j = new Float32Array(Module.HEAPF32.buffer, m + 4, r);
        this.gl.uniformMatrix4fv(this.webGLObject[locationId], false, j);
    }

    public drawElementsEx = (count: any) => {
        this.gl.drawElements(this.gl.TRIANGLES, count, this.gl.UNSIGNED_SHORT, 0);
    }

    public bindFloatBufferEx = (attribute: any, bufferId: any, valueSize: any) => {
        this.gl.bindBuffer(this.gl.ARRAY_BUFFER, this.webGLObject[bufferId]);
        this.gl.enableVertexAttribArray(attribute);
        this.gl.vertexAttribPointer(attribute, valueSize, this.gl.FLOAT, false, 0, 0);
    }

    public bindUShortBufferEx = (bufferId: any) => {
        this.gl.bindBuffer(this.gl.ELEMENT_ARRAY_BUFFER, this.webGLObject[bufferId]);
    }

    public bindTextureEx = (uniformId: any, textureId: any) => {
        this.gl.activeTexture(this.gl.TEXTURE0);
        this.gl.bindTexture(this.gl.TEXTURE_2D, this.webGLObject[textureId]);
        this.gl.uniform1i(this.webGLObject[uniformId], 0);
    }

    public blendFuncEx = () => {
        this.gl.blendFunc(this.gl.SRC_ALPHA, this.gl.ONE);
    }

    public enableBlendEx = () => {
        this.gl.enable(this.gl.BLEND);
    }

    public disableBlendEx = () => {
        this.gl.disable(this.gl.BLEND);
    }

    public useProgramEx = (programId: any) => {
        this.gl.useProgram(this.webGLObject[programId]);
    }

    public depthMaskOnEx = () => {
        this.gl.depthMask(true);
    }

    public depthMaskOffEx = () => {
        this.gl.depthMask(false);
    }

    public clearDepthAndColorEx = () => {
        this.gl.clear(this.gl.DEPTH_BUFFER_BIT | this.gl.COLOR_BUFFER_BIT);
    }
    public clearDepthEx = () => {
        this.gl.clear(this.gl.DEPTH_BUFFER_BIT);
    }
    
  public callBatch = (canvas: HTMLCanvasElement, batchedCalls: any[][]) => {
    const context = this.getContext(canvas);
    for (let i = 0; i < batchedCalls.length; i++) {
      let params = batchedCalls[i].slice(2);
      if (batchedCalls[i][1]) {
        this.callWithContext(context, batchedCalls[i][0], params);
      } else {
        this.setPropertyWithContext(
          context,
          batchedCalls[i][0],
          Array.isArray(params) && params.length > 0 ? params[0] : null);
      }
    }
  }

  private callWithContext = (context: any, method: string, args: any) => {
    return this.serialize(this.prototypes[method].apply(context, args != undefined ? args.map((value) => this.deserialize(method, value)) : []));
  }

  private setPropertyWithContext = (context: any, property: string, value: any) => {
    context[property] = this.deserialize(property, value);
  }

  private getContext = (canvas: HTMLCanvasElement) => {
    if (!canvas) throw new Error('Invalid canvas.');

    const context = this.contexts.get(canvas.id);
    if (!context) throw new Error('Invalid context.');

    return context;
  }

  private deserialize = (method: string, object: any) => {
    if (!this.webGLContext || object == undefined) return object; //deserialization only needs to happen for webGL

    if (object.hasOwnProperty("webGLType") && object.hasOwnProperty("id")) {
      return (this.webGLObject[object["id"]]);
    } else if (Array.isArray(object) && !method.endsWith("v")) {
      return Uint8Array.of(...(object as number[]));
    } else if (typeof(object) === "string" && (method === "bufferData" || method === "bufferSubData")) {
      let binStr = window.atob(object);
      let length = binStr.length;
      let bytes = new Uint8Array(length);
      for (var i = 0; i < length; i++) {
          bytes[i] = binStr.charCodeAt(i);
      }
      return bytes;
    } else
      return object;
  }

  private serialize = (object: any) => {
    if (object instanceof TextMetrics) {
        return { width: object.width };
    }

    if (!this.webGLContext || object == undefined) return object; //serialization only needs to happen for webGL

    const type = this.webGLTypes.find((type) => object instanceof type);
    if (type != undefined) {
      const id = this.webGLObject.length;
      this.webGLObject.push(object);

      return {
        webGLType: type.name,
        id: id
        };
    } else
      return object;
  }
}

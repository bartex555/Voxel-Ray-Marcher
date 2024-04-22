using System;
using Godot;
using Godot.Collections;
using System.Diagnostics;

public partial class ShaderInvoker : Node
{
	Vector2I image_size = new();
	RenderingDevice rd = RenderingServer.CreateLocalRenderingDevice();
	Rid shader;
	Rid pipeline;
	Rid output_texture;
	Rid uniform_set;
	float GlobalTime = 0;

	Stopwatch framePacing = new Stopwatch();
	Array<RDUniform> bindings;


	[Export]
	public TracerOutput texture_rect;
    public override void _Ready()
    {
		image_size.X = (int)ProjectSettings.GetSetting("display/window/size/viewport_width",8);
		image_size.Y = (int)ProjectSettings.GetSetting("display/window/size/viewport_height",8);

		texture_rect.image_size = image_size;
		texture_rect.textureInit();

		SetupCompute();
		Render();
    }

    public override void _Process(double delta)
    {
		//framePacing.Restart();
		UpdateCompute();
		Render((float)delta);
		//framePacing.Stop();
		GD.Print(delta);
    }


    void SetupCompute(){
		//Compiling and setting up the shader
		var shaderFile = GD.Load<RDShaderFile>("res://Ray-Marcher.glsl");
		var shaderBytecode = shaderFile.GetSpirV();
		shader = rd.ShaderCreateFromSpirV(shaderBytecode);
		pipeline = rd.ComputePipelineCreate(shader);

		//Output Texture Buffer
		var fmt = new RDTextureFormat
		{
		Width = (uint)image_size.X,
		Height = (uint)image_size.Y,
		Format = RenderingDevice.DataFormat.R32G32B32A32Sfloat,
		UsageBits = RenderingDevice.TextureUsageBits.CanUpdateBit | RenderingDevice.TextureUsageBits.StorageBit | RenderingDevice.TextureUsageBits.CanCopyFromBit
		};
		var view = new RDTextureView();
		var outputImage = Image.Create(image_size.X,image_size.Y,false,Image.Format.Rgbaf);
		output_texture = rd.TextureCreate(fmt,view,Variant.From(outputImage.GetData()).AsGodotArray<byte[]>());
		var outputTextureUniform = new RDUniform();
		outputTextureUniform.UniformType = RenderingDevice.UniformType.Image;
		outputTextureUniform.Binding = 0;
		outputTextureUniform.AddId(output_texture);

		//Float buffer
		var parameters = new float[] {
			GlobalTime
		};
		var inputBytes = new byte[parameters.Length * sizeof(float)];
		Buffer.BlockCopy(parameters,0,inputBytes,0,inputBytes.Length);
		var parametersBuffer = rd.StorageBufferCreate((uint)inputBytes.Length,inputBytes);
		var parametersUniform = new RDUniform{
			UniformType = RenderingDevice.UniformType.StorageBuffer,
			Binding = 1,
		};
		parametersUniform.AddId(parametersBuffer);

		
		bindings = new  Array<RDUniform> {outputTextureUniform,parametersUniform};
		uniform_set = rd.UniformSetCreate(bindings,shader,0);
		
	}

	void UpdateCompute(){
		//Float buffer
		var parameters = new float[] {
			GlobalTime
		};
		var inputBytes = new byte[parameters.Length * sizeof(float)];
		Buffer.BlockCopy(parameters,0,inputBytes,0,inputBytes.Length);
		var parametersBuffer = rd.StorageBufferCreate((uint)inputBytes.Length,inputBytes);
		var parametersUniform = new RDUniform{
			UniformType = RenderingDevice.UniformType.StorageBuffer,
			Binding = 1,
		};

		parametersUniform.AddId(parametersBuffer);
		bindings[1] = parametersUniform;
		uniform_set = rd.UniformSetCreate(bindings,shader,0);
	}

	void Render(float delta = 0.0f){
		GlobalTime += delta;

		var computeList = rd.ComputeListBegin();

		rd.ComputeListBindComputePipeline(computeList,pipeline);

		rd.ComputeListBindUniformSet(computeList,uniform_set,0);

		rd.ComputeListDispatch(computeList,(uint)(image_size.X/8),(uint)(image_size.Y/8),1);
		
		rd.ComputeListEnd();

		rd.Submit();

		rd.Sync();

		var byteData = rd.TextureGetData(output_texture,0);
		texture_rect.setData(byteData);
	}
}

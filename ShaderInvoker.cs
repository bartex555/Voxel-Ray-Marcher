using System;
using Godot;
using Godot.Collections;
using System.Diagnostics;

public partial class ShaderInvoker : Node
{
	Vector2I image_size = new();
	RenderingDevice rd;
	Rid shader;
	Rid pipeline;
	Rid output_texture;
	Rid uniform_set;
	float GlobalTime = 0;

	Stopwatch framePacing = new Stopwatch();
	Array<RDUniform> bindings;

	//TEMP:
	byte[,,] voxels = new byte[512,512,512];
	[Export] public TracerOutput texture_rect;
	[Export] public Camera3D camera;
    public override void _Ready()
    {
		rd = RenderingServer.GetRenderingDevice();
		//TODO - make it dynamic
		image_size.X = (int)ProjectSettings.GetSetting("display/window/size/viewport_width",8);
		image_size.Y = (int)ProjectSettings.GetSetting("display/window/size/viewport_height",8);

		texture_rect.image_size = image_size;
		texture_rect.textureInit();

		
		SetupCompute();
		Render();
		GD.Print(camera.GetCameraProjection());
    }

    public override void _Process(double delta)
    {
		
		framePacing.Restart();
		UpdateCompute();
		Render((float)delta);
		framePacing.Stop();
		GD.Print(framePacing.ElapsedMilliseconds);
		
    }

	byte[] CameraToBytes(Transform3D t,Projection p){
		Basis basis = t.Basis;
		Vector3 origin = t.Origin;
		float[] unencoded = new float[]{
			basis.X.X, basis.X.Y, basis.X.Z, 1.0f,
			basis.Y.X, basis.Y.Y, basis.Y.Z, 1.0f,
			basis.Z.X, basis.Z.Y, basis.Z.Z, 1.0f,
			origin.X, origin.Y, origin.Z, 1.0f,
			p.X.X, p.X.Y, p.X.Z, p.X.W,
			p.Y.X, p.Y.Y, p.Y.Z, p.Y.W,
			p.Z.X, p.Z.Y, p.Z.Z, p.Z.W,
			p.W.X, p.W.Y, p.W.Z, p.W.W
		};
		byte[] bytes = new byte[unencoded.Length * sizeof(float)];
		Buffer.BlockCopy(unencoded, 0, bytes,0,bytes.Length);
		return bytes;
	}
	
	//Generating Block buffer with LOD (currently 512 voxels in each direction)
	/*
	current direction x -> y -> z
				2 3	   6 7
				0 1	   4 5
	Reference for AND operations:
		7  128
		6  64
		5  32
		4  16
		3  8
		2  4
		1  2
		0  1
	*/
	//LOD scrapped for now
	byte[] PassBlockLOD(byte[,,] voxelsToPass){
		//TEMP AND VERY BAD
		int size = 512;
		int sizeCubed = voxelsToPass.Length;
		/*
		int LOD1_Size = size / 8;
		int LOD1_SizeCubed = sizeCubed / 512;
		int LOD2_Size = LOD1_Size / 8;
		int LOD2_SizeCubed = LOD1_SizeCubed / 512;
		int LOD3_Size = LOD2_Size / 8;
		int LOD3_SizeCubed = LOD2_SizeCubed / 512;
		*/
		
		byte[] finishedBytes = new byte[sizeCubed/* + LOD1_SizeCubed + LOD2_SizeCubed + LOD3_SizeCubed*/];
		for (int x = 0;x < size;x+=2) for (int y = 0;y < size; y+=2) for (int z = 0;z < size; z+=2){
			finishedBytes[size * x + size * y + z] = voxelsToPass[x,y,z]; //0
			finishedBytes[size * x + size * y + z + 1] = voxelsToPass[x+1,y,z]; //1
			finishedBytes[size * x + size * y + z + 2] = voxelsToPass[x,y+1,z]; //2
			finishedBytes[size * x + size * y + z + 3] = voxelsToPass[x+1,y+1,z]; //3
			finishedBytes[size * x + size * y + z + 4] = voxelsToPass[x,y,z+1]; //4
			finishedBytes[size * x + size * y + z + 5] = voxelsToPass[x+1,y,z+1]; //5
			finishedBytes[size * x + size * y + z + 6] = voxelsToPass[x,y+1,z+1]; //6
			finishedBytes[size * x + size * y + z + 7] = voxelsToPass[x+1,y+1,z+1]; //7
		}
		return finishedBytes;
	}
	
    void SetupCompute(){
		//Compiling and setting up the shader
		var shaderFile = GD.Load<RDShaderFile>("res://Ray-Marcher.glsl");
		var shaderBytecode = shaderFile.GetSpirV();
		shader = rd.ShaderCreateFromSpirV(shaderBytecode);
		pipeline = rd.ComputePipelineCreate(shader);
		//End

		//Output Texture Buffer
		var fmt = new RDTextureFormat
		{
		Width = (uint)image_size.X,
		Height = (uint)image_size.Y,
		Format = RenderingDevice.DataFormat.R32G32B32A32Sfloat,
		UsageBits = RenderingDevice.TextureUsageBits.CanUpdateBit | RenderingDevice.TextureUsageBits.StorageBit | RenderingDevice.TextureUsageBits.CanCopyFromBit | RenderingDevice.TextureUsageBits.SamplingBit | RenderingDevice.TextureUsageBits.ColorAttachmentBit
		};
		var view = new RDTextureView();
		var outputImage = Image.Create(image_size.X,image_size.Y,false,Image.Format.Rgbaf);
		output_texture = rd.TextureCreate(fmt,view,Variant.From(outputImage.GetData()).AsGodotArray<byte[]>());
		var outputTextureUniform = new RDUniform();
		outputTextureUniform.UniformType = RenderingDevice.UniformType.Image;
		outputTextureUniform.Binding = 0;
		outputTextureUniform.AddId(output_texture);
		//This is where the magic happens, the rd needs to be global for this to work
		texture_rect.setRID(output_texture);

		//End

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
		//End

		//Camera buffer
		byte[] cameraMatrices = CameraToBytes(camera.GlobalTransform,camera.GetCameraProjection());
		Rid cameraBuffer = rd.StorageBufferCreate((uint)cameraMatrices.Length,cameraMatrices);
		RDUniform cameraUniform = new RDUniform{
			UniformType = RenderingDevice.UniformType.StorageBuffer,
			Binding = 2,
		};
		cameraUniform.AddId(cameraBuffer);
		//End

		//Voxel Buffer
		var vox = PassBlockLOD(voxels);
		var voxBuffer = rd.StorageBufferCreate((uint)vox.Length,vox);
		var voxUniform = new RDUniform{
			UniformType = RenderingDevice.UniformType.StorageBuffer,
			Binding = 3,
		};
		voxUniform.AddId(voxBuffer);
		//End


		bindings = new  Array<RDUniform> {outputTextureUniform,parametersUniform,cameraUniform,voxUniform};
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
		//End

		//Camera buffer
		byte[] cameraMatrices = CameraToBytes(camera.GlobalTransform,camera.GetCameraProjection());
		Rid cameraBuffer = rd.StorageBufferCreate((uint)cameraMatrices.Length,cameraMatrices);
		RDUniform cameraUniform = new RDUniform{
			UniformType = RenderingDevice.UniformType.StorageBuffer,
			Binding = 2,
		};
		cameraUniform.AddId(cameraBuffer);
		//End

		bindings[1] = parametersUniform;
		bindings[2] = cameraUniform;
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

		//Forcing the CPU to wait for the GPU
		//TODO - make it independent, too lazy for that now
		rd.Sync();

		//DEPRECATED LETSSSS GOOOOOOOO
		//The biggest bottleneck in the whole project
		//Maybe stitching several rextures in a pixel shader would work better
		//byte[] byteData = rd.TextureGetData(output_texture,0);
		//texture_rect.setData(byteData);
	}
}

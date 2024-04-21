extends Node

var image_size: Vector2i
var rd = RenderingServer.create_local_rendering_device()
var shader
var pipeline
var uniform_set
var bindings
var output_texture: RID


@onready var texture_rect = $"../Camera3D/TracerOutput"

func _ready():
	image_size.x = ProjectSettings.get_setting("display/window/size/viewport_width",8)
	image_size.y = ProjectSettings.get_setting("display/window/size/viewport_height",8)
	
	texture_rect.image_size = image_size
	texture_rect.textureInit()
	
	setup_compute()
	render()
	
func _process(delta: float) -> void:
	render(delta)

func setup_compute():
	
	var shader_file = load("res://Ray-Marcher.glsl")
	var shader_spirv: RDShaderSPIRV = shader_file.get_spirv()
	shader = rd.shader_create_from_spirv(shader_spirv)
	pipeline = rd.compute_pipeline_create(shader)
	
	# Output Texture Buffer
	var fmt := RDTextureFormat.new()
	fmt.width = image_size.x
	fmt.height = image_size.y
	fmt.format = RenderingDevice.DATA_FORMAT_R32G32B32A32_SFLOAT
	fmt.usage_bits = RenderingDevice.TEXTURE_USAGE_CAN_UPDATE_BIT | RenderingDevice.TEXTURE_USAGE_STORAGE_BIT | RenderingDevice.TEXTURE_USAGE_CAN_COPY_FROM_BIT
	var view := RDTextureView.new()
	var output_image := Image.create(image_size.x, image_size.y, false, Image.FORMAT_RGBAF)
	output_texture = rd.texture_create(fmt, view, [output_image.get_data()])
	var output_tex_uniform := RDUniform.new()
	output_tex_uniform.uniform_type = RenderingDevice.UNIFORM_TYPE_IMAGE
	output_tex_uniform.binding = 0
	output_tex_uniform.add_id(output_texture)
	bindings = [
		output_tex_uniform,
	]
	uniform_set = rd.uniform_set_create(bindings,shader,0)


func render(delta : float = 0.0):
	
	var compute_list = rd.compute_list_begin()
	
	rd.compute_list_bind_compute_pipeline(compute_list,pipeline)
	
	rd.compute_list_bind_uniform_set(compute_list,uniform_set,0)
	@warning_ignore("integer_division")
	rd.compute_list_dispatch(compute_list,image_size.x/8,image_size.y / 8,1)
	
	rd.compute_list_end()
	
	rd.submit()
	
	rd.sync()
	
	var byte_data : PackedByteArray = rd.texture_get_data(output_texture,0)
	texture_rect.setData(byte_data)

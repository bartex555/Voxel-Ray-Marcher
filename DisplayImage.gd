extends TextureRect

var image_size : Vector2i = Vector2i(768,768)

func textureInit():
	var image = Image.create(image_size.x,image_size.y,false,Image.FORMAT_RGBAF)
	var imageTexture = ImageTexture.create_from_image(image)
	texture = imageTexture

func setData(data : PackedByteArray):
	var image := Image.create_from_data(image_size.x,image_size.y,false,Image.FORMAT_RGBAF,data)
	texture.update(image)

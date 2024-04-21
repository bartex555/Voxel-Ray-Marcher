using Godot;
using System;

public partial class TracerOutput : TextureRect
{
public Vector2I image_size = new(500,500);

public void textureInit(){
	Image image = Image.Create(image_size.X,image_size.Y,false,Image.Format.Rgbaf);
	ImageTexture imageTexture = ImageTexture.CreateFromImage(image);
	Texture = imageTexture;
}

public void setData(byte[] data){
	Image image = Image.CreateFromData(image_size.X,image_size.Y,false,Image.Format.Rgbaf,data);
	((ImageTexture)Texture).Update(image);
}
}

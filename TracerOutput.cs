using Godot;
using System;

public partial class TracerOutput : TextureRect
{
public Vector2I image_size = new(500,500);
ImageTexture imageTexture;


//Friendship ended with ImageTexture, now Texture2Drd is my best friend
Texture2Drd RDTexture = new Texture2Drd();


public void textureInit(){
	/*
	Image image = Image.Create(image_size.X,image_size.Y,false,Image.Format.Rgbaf);
	imageTexture = ImageTexture.CreateFromImage(image);
	Texture = imageTexture;
	*/
	Texture = RDTexture;
}

//DEPRECATED
public void setData(byte[] data){
	
	Image image = Image.CreateFromData(image_size.X,image_size.Y,false,Image.Format.Rgbaf,data);
	imageTexture.Update(image);
	
	
}

public void setRID(Rid rid){
	RDTexture.TextureRdRid = rid;
}
}

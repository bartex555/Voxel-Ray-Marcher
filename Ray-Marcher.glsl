#[compute]
#version 450

layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

layout(rgba32f,binding = 0) writeonly uniform image2D rendered_image;

layout(set = 0,binding = 1,std430) readonly buffer Params {
    float time;
} 
params;



void main(){
    float radius = 0.2;
    vec4 pixel = vec4(0.0,0.0,0.0,1.0);
    vec3 color = vec3(sin(params.time)* sin(params.time),cos(params.time) * cos(params.time),(sin(params.time) + 1) / 2);
    ivec2 image_size = imageSize(rendered_image);
    //Cords in range [-1,1]
    vec2 uv = vec2((gl_GlobalInvocationID.xy) / vec2(image_size) * 2 - 1);
    float aspect_ratio = float(image_size.x) / float(image_size.y);
    uv.x *= aspect_ratio;
    vec2 position = vec2(sin(params.time) / 2,cos(params.time) / 2);

    if ((position.x - uv.x)*(position.x - uv.x) + (position.y - uv.y) * (position.y - uv.y) < radius*radius){
        pixel.xyz = color;
    }
    
    imageStore(rendered_image,ivec2(gl_GlobalInvocationID.xy),pixel);
}

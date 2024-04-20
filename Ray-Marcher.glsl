#[compute]
#version 450

layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

layout(rgba32f,binding = 0) uniform image2D rendered_image;

void main(){
    vec4 pixel = vec4(0.0,0.0,0.0,1.0);

    ivec2 image_size = imageSize(rendered_image);
    // Cords in range [-1,1]
    vec2 uv = vec2((gl_GlobalInvocationID.xy) / vec2(image_size) * 2 - 1);
    float aspect_ratio = float(image_size.x) / float(image_size.y);
    uv.x *= aspect_ratio;

    pixel.x = (uv.x + 1) / 2;
    pixel.y = (uv.y + 1) / 2;
    pixel.z = (uv.y + 1) / 2;
    imageStore(rendered_image,ivec2(gl_GlobalInvocationID.xy),pixel);
}

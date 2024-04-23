#[compute]
#version 450

layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

layout(rgba32f,binding = 0) uniform image2D rendered_image;

layout(set = 0,binding = 1,std430) readonly buffer Params {
    float time;
} params;


layout (set = 0,binding = 2,std430) readonly buffer CameraData{
    mat4 camera_in_world;
    mat4 camera_projection;
} camera_data;

struct Ray{
    vec3 origin;
    vec3 direction;
    float energy;
};

Ray CreateRay(vec3 origin,vec3 direction,float energy){
    Ray ray;
    ray.origin = origin;
    ray.direction = direction;
    ray.energy = energy;
    return ray;
}

Ray constructFromCamera(vec2 uv){
    mat4 camera_projection = camera_data.camera_projection;
    camera_projection[1][1] = camera_projection[0][0];
    mat4 inverse_projection = inverse(camera_projection);
    mat4 camera_in_world = camera_data.camera_in_world;

    vec3 origin = camera_in_world[3].xyz;

    vec3 direction = (inverse_projection * vec4(uv,0,1.0)).xyz;
    direction = (camera_in_world * vec4(direction,0.0)).xyz;
    direction = normalize(direction);
    return CreateRay(origin,direction,1.0);
}



void main(){
    float radius = 0.2;
    vec4 pixel = vec4(0.0,0.0,0.0,1.0);
    ivec2 image_size = imageSize(rendered_image);
    //Cords in range [-1,1]
    vec2 uv = vec2((gl_GlobalInvocationID.xy) / vec2(image_size) * 2 - 1);
    float aspect_ratio = float(image_size.x) / float(image_size.y);
    uv.x *= aspect_ratio;
    
    Ray ray = constructFromCamera(uv);

    pixel.xyz = vec3(ray.direction.x,ray.direction.y,ray.direction.z);
    
    imageStore(rendered_image,ivec2(gl_GlobalInvocationID.xy),pixel);
}

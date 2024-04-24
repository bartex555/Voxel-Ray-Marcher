#[compute]
#version 450
#extension GL_ARB_gpu_shader_int64 : enable

#define GRID_CORDS 128


layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

layout(rgba32f,binding = 0) uniform image2D rendered_image;

layout(set = 0,binding = 1,std430) readonly buffer Params {
    float time;
} params;


layout (set = 0,binding = 2,std430) readonly buffer CameraData{
    mat4 camera_in_world;
    mat4 camera_projection;
} camera_data;

//The world for now is 128 x 128 x 128 and each "normal size" block is divided into 64 voxels
layout (set = 0,binding = 3,std430) readonly buffer VoxelData {
    uint64_t[256][256][256] blocks;
    /*
    uint64_t[64][64][64] LOD_1;
    uint64_t[8][8][8] LOD_2;
    uint64_t LOD_3;
    */
} voxel_data;

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

struct RayHit {
    vec3 position;
    vec3 dst;
    vec3 normal;
    vec3 color;

};

RayHit CreateHit(vec3 position,vec3 normal,vec3 color){
    RayHit newHit;
    newHit.position = position;
    newHit.normal = normal;
    newHit.color = color;
    return newHit;
}

Ray constructFromCamera(vec2 uv){
    mat4 camera_projection = camera_data.camera_projection;
    camera_projection[0][0] = camera_projection[1][1]; //Fixes the pov for some reason
    mat4 inverse_projection = inverse(camera_projection);
    mat4 camera_in_world = camera_data.camera_in_world;

    vec3 origin = camera_in_world[3].xyz;

    vec3 direction = (inverse_projection * vec4(uv,0,1.0)).xyz;
    direction = (camera_in_world * vec4(direction,0.0)).xyz;
    direction = normalize(direction);
    return CreateRay(origin,direction,1.0);
}


//TODO
float GetLargestPossibleStep(Ray ray, float gridStep){
    return 0;
}

Ray traceToGrid(Ray ray){
    if (0 < ray.origin.x && ray.origin.x < GRID_CORDS && 0 < ray.origin.y && ray.origin.y < GRID_CORDS && 0 < ray.origin.z && ray.origin.z < GRID_CORDS){
        return ray;
    }else{
        float t = -1;
        vec3 hitPoz;
        if (ray.direction.x < 0){
            t = (GRID_CORDS - ray.origin.x) / ray.direction.x;
        }else{
            t = (-ray.origin.x) / ray.direction.x;
        }
        if (t > 0){
            hitPoz = ray.origin + ray.direction * t;
            if (0 < hitPoz.y && hitPoz.y < GRID_CORDS && 0 < hitPoz.z && hitPoz.z < GRID_CORDS){
                ray.origin = hitPoz;
                return ray;
            }
        }
        if (ray.direction.y < 0){
            t = (GRID_CORDS - ray.origin.y) / ray.direction.y;
        }else{
            t = (-ray.origin.y) / ray.direction.y;
        }
        if (t > 0){
            hitPoz = ray.origin + ray.direction * t;
            if (0 < hitPoz.x && hitPoz.x < GRID_CORDS && 0 < hitPoz.z && hitPoz.z < GRID_CORDS){
                ray.origin = hitPoz;
                return ray;
            }
        }
         if (ray.direction.z < 0){
            t = (GRID_CORDS - ray.origin.z) / ray.direction.z;
        }else{
            t = (-ray.origin.z) / ray.direction.z;
        }
        if (t > 0){
            hitPoz = ray.origin + ray.direction * t;
            if (0 < hitPoz.x && hitPoz.x < GRID_CORDS && 0 < hitPoz.y && hitPoz.y < GRID_CORDS){
                ray.origin = hitPoz;
                return ray;
            }
        }
    }
    ray.energy = 0;
    return ray;
}




void main(){
    float radius = 0.2;
    vec4 pixel = vec4(0.0,0.0,0.0,1.0);
    uint64_t test = 20;
    ivec2 image_size = imageSize(rendered_image);
    //Cords in range [-1,1]
    vec2 uv = vec2((gl_GlobalInvocationID.xy) / vec2(image_size) * 2 - 1);
    float aspect_ratio = float(image_size.x) / float(image_size.y);
    uv.x *= aspect_ratio;
    
    Ray ray = constructFromCamera(uv);
    ray = traceToGrid(ray);
    if (ray.energy <= 0){
    pixel.xyz = vec3(ray.direction.x,ray.direction.y,ray.direction.z);
    }else{
        pixel.xyz = vec3(0.5,0.5,0.5);
    }
    imageStore(rendered_image,ivec2(gl_GlobalInvocationID.xy),pixel);
}

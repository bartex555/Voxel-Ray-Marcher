extends CharacterBody3D

const SPEED = 25.0
const LOOKSPEED = 0.05

@export var Head : Camera3D

func _physics_process(delta):
	var movement_vector = Input.get_vector("movement_left","movement_right","movement_up","movement_down")
	var pan_vector = Input.get_vector("pan_left","pan_right","pan_up","pan_down")
	var up_down_axis = Input.get_axis("down_movement","up_movement")
	
	#rotation
	rotate_y(-pan_vector.x * LOOKSPEED)
	Head.rotate_x(-pan_vector.y * LOOKSPEED)
	if -90 > Head.rotation_degrees.x:
		Head.rotation_degrees.x = -90
	if Head.rotation_degrees.x > 90:
		Head.rotation_degrees.x = 90
	
	#movement
	var direction = (transform.basis * Vector3(movement_vector.x,0,movement_vector.y)).normalized()
	velocity.x = direction.x * SPEED
	velocity.z = direction.z * SPEED
	velocity.y = up_down_axis * SPEED
	
	
	move_and_slide()
	

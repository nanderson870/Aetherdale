using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Blastbuggy : MonoBehaviour
{
    public Transform playerPosition;
    public Transform projectileSpawnPoint;

    public Projectile projectile;
    public float projectileSpeed = 50.0F;
    public float driveCameraSensitivity = 3.0F;
    public float engageCameraSensitivity = 1.0F;
    public CameraContext cameraContext;
    public float maxSpeed = 48.0F;
    public float accelerationRate = 24.0F;
    public float decelerationLerpValue = 4.0F;
    public float maxSpeedReverseMult = 0.5F;
    public float driveModeTurnSpeed = 3.0F;
    public float engageModeTurnSpeed = 4.0F;


    ControlledEntity driver;
    Rigidbody body;
    Animator animator;

    Vector3 input;
    float mouseXInput;
    float mouseYInput;

    bool engaged = false;
    bool midswitch = false;

    Vector3 velocity = new Vector3();


    // Start is called before the first frame update
    protected void Start()
    {
        body = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
    }

    // Update is called once per frame
    protected void Update()
    {
        if (driver != null)
        {
            driver.transform.position = playerPosition.position;
            driver.transform.rotation = playerPosition.rotation;
        }
        //Shoot();
    }

    void FixedUpdate()
    {
        ApplyMovement();
    }

    void LateUpdate()
    {
        Animate();
    }

    private void SwitchModes()
    {
        if (midswitch)
        {
            return;
        }

        midswitch = true;
        engaged = !engaged;

        if (engaged)
        {
            animator.speed = 1.0F;
            //networkAnimator.SetTrigger("enter_engaged_mode");
        }
        else
        {
            //networkAnimator.SetTrigger("enter_drive_mode");
        }

    }

    public void SwitchComplete()
    {
        midswitch = false;
    }

    public void Interact (ControlledEntity interactingEntity)
    {
        driver = interactingEntity;
        //PlayerCamera.Singleton.SetContext(cameraContext);

        //interactingPlayer.SetProcessInputFunction(ProcessInput);
    }

    public void DismountDriver()
    {
        //driver.ResetDefaultProcessInputFunction();
        //PlayerCamera.Singleton.SetContext(driver.GetCameraContext());
        driver = null;
    }

    void ProcessInput()
    {
        // Zero out input and check for dismount, return before any input is evaluated if dismounting
        input = new Vector3();
        if (Input.GetKeyDown(KeyCode.F))
        {
            DismountDriver();
            return;
        }

        mouseXInput = Input.GetAxis("Mouse X");
        mouseYInput = Input.GetAxis("Mouse Y");


        int zInput = 0;
        if(Input.GetKey(KeyCode.W))
        {
            zInput += 1;
        }

        if(Input.GetKey(KeyCode.S))
        {
            zInput -= 1;
        }

        input.z = zInput;
        
        if (engaged)
        {
            Aim(mouseXInput, mouseYInput);
        }
        else
        {
            cameraContext.AddRotation(new Vector2(mouseXInput * driveCameraSensitivity, -mouseYInput * driveCameraSensitivity));
        }

        if(Input.GetKey(KeyCode.T))
        {
            SwitchModes();
        }

        if (Input.GetMouseButtonDown(0) && engaged && !midswitch)
        {
            Shoot();
        }

    }
    
    void Aim(float x, float y)
    {
        cameraContext.AddRotation(new Vector2(mouseXInput * engageCameraSensitivity, 0.0F));
    }

    void ApplyMovement()
    {
        if (!engaged && !midswitch)
        {
            if ((input.z > 0 || input.z < 0))
            {
                velocity.z += input.z * accelerationRate * Time.deltaTime;
            }
            else
            {
                velocity.z = Mathf.Lerp(velocity.z, 0.0F, decelerationLerpValue * Time.deltaTime);
            }

            velocity.z = Mathf.Clamp(velocity.z, -maxSpeed * maxSpeedReverseMult, maxSpeed);
        }
        else
        {
            velocity.z = 0.0F;
        }

        Vector3 globalVelocity = transform.TransformVector(velocity);

        body.linearVelocity = new Vector3(globalVelocity.x, body.linearVelocity.y, globalVelocity.z);


        // handle rotation
        if (driver != null)
        {
            Quaternion driverCameraRot = Quaternion.Euler(0, driver.GetComponent<Camera>().transform.eulerAngles.y, 0);
            if (!engaged)
            {
                // drive-mode rotation, turn inversely proportional to speed
                if (Mathf.Abs(velocity.z) > 0)
                {
                    float proportionOfMaxSpeed = maxSpeed / Mathf.Abs(velocity.z);
                    transform.rotation = Quaternion.Slerp(transform.rotation, driverCameraRot, (Time.deltaTime * driveModeTurnSpeed) / proportionOfMaxSpeed);
                }
            }
            else
            {
                //engage-mode rotation, slow pivot
                transform.rotation = Quaternion.Slerp(transform.rotation, driverCameraRot, Time.deltaTime * engageModeTurnSpeed);
            }
        }

    }

    void Animate()
    {
        animator.SetFloat("zVelocity", velocity.z);

        if (!engaged && Mathf.Abs(velocity.z) > 0.05F)
        {
            animator.speed = velocity.z > 0 ? velocity.z / maxSpeed : Mathf.Abs(velocity.z) / (maxSpeed * maxSpeedReverseMult);
        }
    }

    void Shoot()
    {

        if (!engaged)
        {
            return;
        }

        Projectile.Create(projectile, projectileSpawnPoint, gameObject, new Vector3(0.0F, 0.0F, projectileSpeed));
    }

}

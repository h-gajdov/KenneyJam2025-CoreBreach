using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Player : Entity {
    public float moveSmoothTime = 1f;
    public float enemyInRangeRadius = 10f;
    public float pickupsInRangeRadius = 5f;
    public float timeOfLastKilledEnemy;
    public float powerMeterValue = 0;
    public float powerMeterDecaySpeed = 4f;
    public float ultimateBulletDamage = 100f;
    public float ultimateFireRate = 5f;
    public float coins = 40f;
    public int score = 0;
    public GameObject ultimateBulletPrefab;
    public MissleCrosshair missleCrosshair;
    public Slider powerMeterSlider;
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI scoreText;

    Vector2 velocity = Vector2.zero;
    Animator powerMeterAnim;
    float nextTimeToSeekForTargets;
    float nextTimeToFireUltimateBullet;
    bool ultimateIsOn = false;
    public bool inPlatform = false;

    public static Player instance;

    private void Awake() {
        health = startHealth;
        if (instance == null) instance = this;
        else {
            Destroy(this);
            return;
        }
    }

    private void Start() {
        powerMeterAnim = powerMeterSlider.GetComponent<Animator>();
        InvokeRepeating("TargetEnemiesInRange", 0f, 1f);
    }

    private void Update() {
        PickUpPickupsInRange();

        Move();
        if (Input.GetMouseButton(0) && Time.time >= nextTimeToFire && !inPlatform) Shoot();

        if(missleCrosshair.target == null) {
            if(Time.time >= nextTimeToSeekForTargets) {
                TargetEnemiesInRange();
                if(missleCrosshair.target != null) nextTimeToSeekForTargets = Time.time + 20f;
            }
        }
        else if (missleCrosshair.image.enabled && Input.GetMouseButtonDown(1) && Time.time >= nextTimeToFireMissile) FireMissileAt(missleCrosshair.target);
    }

    private void FixedUpdate() {
        rb.velocity = velocity;
    }

    protected override void Move() {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        Vector2 input = x * transform.right + y * transform.up;
        if (input.magnitude > 0.01f) {
            velocity = Vector2.Lerp(velocity, input.normalized * moveSpeed, Time.deltaTime * moveSmoothTime);
            motors.TurnOnMotors();
        } else {
            velocity = Vector2.Lerp(velocity, Vector2.zero, Time.deltaTime * moveSmoothTime);
            motors.TurnOffMotors();
        }

        if (powerMeterValue < 1) powerMeterValue -= powerMeterDecaySpeed * Time.deltaTime;
        else {
            powerMeterAnim.SetBool("isBlinking", true);
            if(Input.GetMouseButtonDown(1)) {
                StopAllCoroutines();
                StartCoroutine(UltimateAttack());
                powerMeterAnim.SetBool("isBlinking", false);
                powerMeterValue = 0;
            }
        }

        powerMeterValue = (powerMeterValue < 0) ? 0 : powerMeterValue;
        powerMeterSlider.value = powerMeterValue;
    }

    public override void Die() {
        AudioManager.Play("Lose");
        this.enabled = false;
        GetComponent<BoxCollider2D>().enabled = false;
        shipBody.gameObject.SetActive(false);
        UIManager.instance.SetLoseScreen(score);
    }

    public new void Shoot() {
        base.Shoot();
        AudioManager.Play("Laser1");
    }

    public ITarget[] GetEnemiesInRange() {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, enemyInRangeRadius);
        List<ITarget> enemies = new List<ITarget>();
        foreach(var hit in hits) {
            ITarget enemy;
            if (hit.TryGetComponent<ITarget>(out enemy)) {
                if (enemy.GetTransform().gameObject.CompareTag("Player") || enemy.GetTransform().gameObject.CompareTag("PowerCore")) continue;
                enemies.Add(enemy);
            }
        }
        return enemies.ToArray();
    }

    public void PickUpPickupsInRange() {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, pickupsInRangeRadius);
        List<Pickup> pickups = new List<Pickup>();
        foreach(var hit in hits) {
            Pickup pickup;
            if(hit.TryGetComponent<Pickup>(out pickup)) {
                pickup.StartMovingToPlayer();
            }
        }
    }

    private void TargetEnemiesInRange() {
        ITarget[] enemiesInRange = GetEnemiesInRange();
        if (enemiesInRange.Length == 0) return;
        missleCrosshair.SetTarget(enemiesInRange[0].GetTransform());
    }

    public void AddToPowerMeter() {
        if (ultimateIsOn) return;
        powerMeterValue += 0.4f;
    }

    public void AddScore(int value) {
        score += value;
        scoreText.text = $"Score: {score}";
    }

    public void AddCoins(float amount) {
        coins += amount;
        coinsText.text = ": " + coins;
    }

    public void Pay(float amount) {
        coins -= amount;
        if (coins < 0) coins = 0;
        coinsText.text = ": " + coins;
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.red;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, enemyInRangeRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pickupsInRangeRadius);
    }

    private IEnumerator UltimateAttack() {
        float started = Time.time;
        float elapsed = 0;
        ultimateIsOn = true;
        while(elapsed < 3f) {
            if(Time.time >= nextTimeToFireUltimateBullet) {
                Bullet bullet = Instantiate(ultimateBulletPrefab, transform.position, shipBody.rotation).GetComponent<Bullet>();
                bullet.shotFrom = gameObject;
                bullet.damage = ultimateBulletDamage;
                Destroy(bullet.gameObject, 10f);
                nextTimeToFireUltimateBullet = Time.time + (1 / ultimateFireRate);
            }
            elapsed = Time.time - started;
            yield return null;
        }
        yield return new WaitForSeconds(2f);
        ultimateIsOn = false;
    }
}

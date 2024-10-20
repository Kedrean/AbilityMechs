using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDrainScript : MonoBehaviour
{
    public float maxHealth = 100f;      // Maximum health of the player
    public float currentHealth;         // Current health of the player
    public float drainDuration = 3f;    // Duration of the drain ability
    public float drainRadius = 5f;      // Radius for detecting enemies
    public float damagePerSecond = 10f; // Damage per second to enemies
    public float healPercentage = 0.5f; // Percentage of damage dealt to heal
    public LayerMask enemyLayer;        // Layer that defines what is considered an enemy
    public ParticleSystem drainEffect;  // Visual effect for drain

    private bool isDraining = false;
    private Dictionary<EnemyScript, LineRenderer> activeLines = new Dictionary<EnemyScript, LineRenderer>(); // Tracks lines connecting player to enemies

    void Start()
    {
        // Initialize player's current health
        currentHealth = maxHealth;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.W) && !isDraining)
        {
            StartCoroutine(DrainAbility());
        }
    }

    IEnumerator DrainAbility()
    {
        isDraining = true;
        float elapsedTime = 0f;

        // Start particle effect
        if (drainEffect != null)
        {
            drainEffect.Play();
        }

        while (elapsedTime < drainDuration)
        {
            // Find all enemies within the radius
            Collider[] enemies = Physics.OverlapSphere(transform.position, drainRadius, enemyLayer);

            foreach (Collider enemy in enemies)
            {
                // Apply damage to enemies and heal the player
                EnemyScript enemyScript = enemy.GetComponent<EnemyScript>();
                if (enemyScript != null)
                {
                    float damage = damagePerSecond * Time.deltaTime;
                    enemyScript.TakeDamage(damage); // Deal damage to the enemy

                    Heal(damage * healPercentage);  // Heal player based on the damage dealt

                    // Visual Effect: Line Renderer between player and enemy
                    if (!activeLines.ContainsKey(enemyScript))
                    {
                        LineRenderer line = CreateLineRenderer();
                        activeLines.Add(enemyScript, line);
                    }

                    // Update the positions of the line renderer
                    LineRenderer lr = activeLines[enemyScript];
                    lr.SetPosition(0, transform.position);           // Start at player
                    lr.SetPosition(1, enemy.transform.position);     // End at enemy
                }
            }

            // Clean up line renderers for enemies that have left range or died
            List<EnemyScript> enemiesToRemove = new List<EnemyScript>();
            foreach (var pair in activeLines)
            {
                if (!pair.Key || !Physics.CheckSphere(pair.Key.transform.position, drainRadius, enemyLayer))
                {
                    Destroy(pair.Value.gameObject); // Remove line renderer
                    enemiesToRemove.Add(pair.Key);  // Mark enemy for removal
                }
            }
            foreach (var enemy in enemiesToRemove)
            {
                activeLines.Remove(enemy); // Remove from the dictionary
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Stop the particle effect after drain ends
        if (drainEffect != null)
        {
            drainEffect.Stop();
        }

        // Destroy all remaining line renderers after the ability ends
        foreach (var line in activeLines.Values)
        {
            Destroy(line.gameObject);
        }
        activeLines.Clear();

        isDraining = false;
    }

    void Heal(float amount)
    {
        // Heal the player by the specified amount, but not over max health
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        Debug.Log("Player healed by: " + amount + ". Current Health: " + currentHealth);
    }

    // Creates a line renderer for visualizing the connection to enemies
    private LineRenderer CreateLineRenderer()
    {
        GameObject lineObj = new GameObject("DrainLine");
        LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();

        // Set start and end width for the beam
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;

        // Set a material for the line renderer (use a basic material)
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

        // Optionally, set color to resemble Fiddlesticks' drain (dark red)
        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.black;

        return lineRenderer;
    }

    // Optional: Visualize the drain radius in the editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, drainRadius);
    }
}

// which side of the fight a path combatant is on. enemies (the wave) advance toward the
// objective and attack plants. friendlies (minions, hypnotized insects) hold or move back
// and attack enemies. friendlies are kept out of Insect.allInsects so every plant and AoE
// that iterates allInsects ignores them automatically (no friendly fire)
public enum Team { Enemy, Friendly }

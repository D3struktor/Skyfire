using UnityEngine;

public class FpsAnimationController : MonoBehaviour
{
    private Animator anim;
    private int xmove = Animator.StringToHash("xmove");
    private int ymove = Animator.StringToHash("ymove");

    private void Awake()
    {
        anim = GetComponent<Animator>(); // Pobierz komponent Animatora
    }

    // Funkcja aktualizująca animacje na podstawie osi ruchu
    public void PlayerAnimation(Vector2 movementAxis)
    {
        anim.SetFloat(xmove, movementAxis.x); // Ruch na osi poziomej
        anim.SetFloat(ymove, movementAxis.y); // Ruch na osi pionowej
    }
}

using TMPro;
using UnityEngine;

namespace YoloHolo.YoloLabeling
{
    public class ObjectLabelController : MonoBehaviour
    {
        [SerializeField] private TextMeshPro textMesh;

        [SerializeField] private GameObject contentParent;

        public void SetText(string text)
        {
            textMesh.text = text;
        }

        private void Start()
        {
            var lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.SetPosition(0, contentParent.transform.position);
            lineRenderer.SetPosition(1, transform.position);
        }
    }
}

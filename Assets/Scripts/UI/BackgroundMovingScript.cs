using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UI
{
    public class BackgroundMovingScript : MonoBehaviour
    {
        [SerializeField] private GameObject backgroundGO;
        [SerializeField] private float bottomCorner = -11;
        [SerializeField] private float topCorner = 11;

        private List<GameObject> _listOfBackgrounds;

        private void LateUpdate()
        {
            if (_listOfBackgrounds == null)
            {
                _listOfBackgrounds = new List<GameObject>
                {
                    Instantiate(backgroundGO, Vector3.zero, Quaternion.identity)
                };
            }

            _listOfBackgrounds.ForEach(go =>
            {
                var goPosition = go.transform.position;
                goPosition.y -= 0.01f;

                go.transform.position = goPosition;
            });

            var removeCandidate = _listOfBackgrounds.FirstOrDefault(go => go.transform.position.y <= bottomCorner);
            var needAddBacground = _listOfBackgrounds.FirstOrDefault(go => go.transform.position.y <= 0f) != null;

            if (needAddBacground && _listOfBackgrounds.Count < 2)
            {
                var goInstance = Instantiate(
                    backgroundGO,
                    new Vector3(0, topCorner, 0),
                    Quaternion.identity);

                _listOfBackgrounds.Add(goInstance);
            }

            if (removeCandidate != null)
            {
                _listOfBackgrounds.Remove(removeCandidate);
                Destroy(removeCandidate);
            }
        }
    }
}
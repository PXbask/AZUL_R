using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AZUL
{
    public class PieceTokenDataBinding : MonoBehaviour
    {
        [SerializeField]
        private Material emptyTokenMat;

        [SerializeField]
        private Material blueTokenMat;

        [SerializeField]
        private Material yellowTokenMat;

        [SerializeField]
        private Material redTokenMat;

        [SerializeField]
        private Material blackTokenMat;

        [SerializeField]
        private Material whiteTokenMat;

        public Material GetMaterial(PieceColorType pieceTokenType)
        {
            switch (pieceTokenType)
            {
                case PieceColorType.SpecialToken:
                    return emptyTokenMat;
                case PieceColorType.Blue:
                    return blueTokenMat;
                case PieceColorType.Yellow:
                    return yellowTokenMat;
                case PieceColorType.Red:
                    return redTokenMat;
                case PieceColorType.Black:
                    return blackTokenMat;
                case PieceColorType.White:
                    return whiteTokenMat;
                default:
                    return null;
            }
        }
    }
}

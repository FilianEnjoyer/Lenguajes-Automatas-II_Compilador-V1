using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Editordetexto
{
    public class NodoExpresion
    {
        public string Valor;
        public NodoExpresion Izquierdo;
        public NodoExpresion Derecho;

        public NodoExpresion(string valor, NodoExpresion izquierdo = null, NodoExpresion derecho = null)
        {
            Valor = valor;
            Izquierdo = izquierdo;
            Derecho = derecho;
        }
    }
}

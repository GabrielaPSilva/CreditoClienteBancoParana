using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public class CartaoCredito
    {
        public Guid Id { get; set; }
        public string NumCartao { get; set; }
        public int CVV { get; set; }
        public string DataExp { get; set; }
        public PropostaCredito Proposta { get; set; }

        public CartaoCredito(Guid id, int cvv, string numCartao, string dataExp, PropostaCredito proposta)
        {
            Id = id;
            CVV = cvv;
            NumCartao = numCartao;
            DataExp = dataExp;
            Proposta = proposta;
        }

        public override string ToString()
        {
            return $"Num. Cartão {NumCartao}, DataExp. {DataExp}, CVV {CVV}, Limite {Proposta.Limite}, Aprovado";
        }
    }
}

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
        public string CVV { get; set; }
        public int MesExp { get; set; }
        public int AnoExp { get; set; }
        public PropostaCredito Proposta { get; set; }

        public CartaoCredito(Guid id, string cvv, string numCartao, int mesExp, int anoExp, PropostaCredito proposta)
        {
            Id = id;
            CVV = cvv;
            NumCartao = numCartao;
            MesExp = mesExp;
            AnoExp = anoExp;
            Proposta = proposta;
        }

        public override string ToString()
        {
            return $"Cartão de Crédito gerado com sucesso: Num. Cartão {NumCartao}, DataExp. {(MesExp + "/" + AnoExp)}, CVV {CVV}, Limite {Proposta.Limite}, Aprovado";
        }
    }
}

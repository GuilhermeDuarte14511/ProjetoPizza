# Prontidão fiscal

O sistema imprime comprovantes **não fiscais** em impressoras ESC/POS de rede. A emissão de NFC-e permanece desabilitada até a pizzaria fornecer e validar os dados fiscais reais; o sistema não gera XML, chave, QR Code ou protocolo fictícios.

## Dados necessários para habilitar NFC-e

- UF e credenciamento do contribuinte para NFC-e;
- regime tributário, inscrição estadual e dados completos do estabelecimento;
- certificado digital válido e sua cadeia, armazenados fora do repositório;
- CSC e identificador do CSC obtidos no ambiente da SEFAZ, também fora do repositório;
- ambiente inicial de homologação, endpoints e regras vigentes da UF;
- cadastro fiscal dos produtos, incluindo NCM, CFOP, CSOSN/CST, unidade tributável e tributação aplicável;
- política para contingência, cancelamento, inutilização, guarda do XML e impressão do DANFE NFC-e.

O Portal Nacional descreve a solução de NFC-e como o conjunto responsável por geração, transmissão, autorização, impressão e guarda, e publica separadamente o MOC e as especificações do DANFE/QR Code. Referências oficiais:

- https://www.nfe.fazenda.gov.br/portal/listaConteudo.aspx?tipoConteudo=ndIjl+iEFdE=
- https://www.confaz.fazenda.gov.br/legislacao/arquivo-manuais/moc7-visao-geral.pdf

## Configuração preparada

As chaves `Fiscal__Enabled`, `Fiscal__Provider`, `Fiscal__State`, `Fiscal__Environment` e `Fiscal__CertificatePath` reservam o limite de configuração. Nenhuma delas contém segredo no repositório. `Fiscal__Enabled` deve continuar `false` até existir uma implementação homologada e testes com a SEFAZ da UF.

O fluxo de balcão imprime somente um **comprovante sem valor fiscal**, com itens, valores, pagamento e troco. A comanda destinada à cozinha é operacional e não contém valores. Consulte `docs/counter-checkout-printing.md`.

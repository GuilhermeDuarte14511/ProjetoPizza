import { ApiError } from '../api/httpClient'

const domainTranslations: Array<[RegExp, string]> = [
  [/category does not exist/i, 'A categoria informada não existe.'],
  [/linked table does not exist/i, 'A mesa vinculada não existe.'],
  [/customer tablet is unavailable/i, 'O código informado não corresponde a um tablet disponível.'],
  [/customer tablet is not linked to a table/i, 'Este tablet ainda não foi vinculado a uma mesa no painel administrativo.'],
  [/linked table does not have an open session/i, 'Abra o atendimento da mesa vinculada antes de ativar o tablet.'],
  [/orders are unavailable while the cash register is closed/i, 'Os pedidos estão temporariamente indisponíveis porque o caixa está fechado.'],
  [/orders can only be submitted while the table session is open/i, 'A mesa já solicitou a conta e não aceita novos pedidos.'],
  [/selected product is unavailable/i, 'Um item do carrinho não está mais disponível. Atualize o cardápio.'],
  [/selected pizza flavor is unavailable/i, 'Um dos sabores selecionados não está mais disponível.'],
  [/selected pizza crust is unavailable/i, 'A borda selecionada não está mais disponível.'],
  [/split crust halves must be different/i, 'Escolha dois recheios diferentes para dividir a borda.'],
  [/first crust half is required/i, 'Selecione o recheio da primeira metade da borda.'],
  [/there is already an open call for this reason/i, 'Já existe uma solicitação aberta com esse mesmo motivo.'],
  [/table already belongs to an open session/i, 'A mesa já possui um atendimento aberto.'],
  [/one or more selected tables already belong to an open session/i, 'Uma das mesas selecionadas já possui atendimento aberto. Atualize a lista e tente novamente.'],
  [/select at least one table for seating/i, 'Selecione ao menos uma mesa para acomodar o cliente.'],
  [/selected tables do not have enough capacity/i, 'As mesas selecionadas não possuem lugares suficientes.'],
  [/one or more selected tables are unavailable/i, 'Uma das mesas selecionadas não está mais disponível. Atualize a lista.'],
  [/a table with service history cannot be deleted/i, 'Esta mesa já possui histórico de atendimento. Desative-a para preservar os registros.'],
  [/unlink all devices from the table before deleting it/i, 'Desvincule os tablets e demais dispositivos antes de excluir a mesa.'],
  [/insufficient stock for/i, 'Não há estoque disponível para todos os itens deste pedido.'],
  [/a bill requires at least one valid order/i, 'A conta precisa ter ao menos um pedido válido.'],
  [/cash payments require an open cash shift/i, 'Abra o caixa antes de registrar pagamentos em dinheiro.'],
  [/an open cash shift already exists/i, 'Já existe um turno de caixa aberto. Atualize a página para consultar o turno atual.'],
  [/cash register is unavailable/i, 'O caixa selecionado não está disponível para abertura.'],
  [/split payment must contain between 2 and 50 people/i, 'Divida a conta entre 2 e 50 pessoas.'],
  [/split total must match the bill remaining amount/i, 'A soma das partes precisa corresponder ao saldo da conta. Recalcule a divisão.'],
  [/selected payment method is unavailable/i, 'Uma das formas de pagamento selecionadas não está disponível.'],
  [/received amount cannot be lower than payment amount/i, 'O valor recebido não pode ser menor que a parte da conta.'],
  [/payment method does not allow change/i, 'A forma de pagamento selecionada não permite troco.'],
  [/payment method requires an external reference/i, 'Informe a referência ou autorização do pagamento.'],
  [/configure an online network printer before printing/i, 'Nenhuma impressora de rede está configurada e online. Configure-a em Configurações > Impressoras ou use a impressão pelo navegador.'],
  [/unsupported manual cash movement type/i, 'O tipo de movimentação informado não é permitido.'],
  [/unknown product type/i, 'O tipo de produto informado é inválido.'],
  [/unknown pizza flavor type/i, 'O tipo de sabor informado é inválido.'],
  [/unknown .* transition/i, 'Essa mudança de status não é permitida.'],
  [/already exists/i, 'Já existe um registro com os dados informados.'],
  [/not found/i, 'O registro solicitado não foi encontrado.'],
]

function translateDomainMessage(message: string) {
  return domainTranslations.find(([pattern]) => pattern.test(message))?.[1]
}

export function getUserErrorMessage(error: unknown) {
  if (error instanceof ApiError) {
    const translated = translateDomainMessage(error.message)
    if (translated) return translated

    if (error.status === 0) return 'Não foi possível conectar à API. Verifique se o serviço está em execução.'
    if (error.status === 400) return 'Alguns dados são inválidos. Revise os campos e tente novamente.'
    if (error.status === 401) return 'Sua sessão expirou. Entre novamente para continuar.'
    if (error.status === 403) return 'Você não possui permissão para realizar esta ação.'
    if (error.status === 404) return 'O registro solicitado não foi encontrado.'
    if (error.status === 409) return 'A operação entra em conflito com o estado atual do registro.'
    if (error.status === 422) return 'Não foi possível concluir porque uma regra de negócio não foi atendida.'
    if (error.status >= 500) {
      return error.traceId
        ? `O servidor encontrou um erro. Informe o código ${error.traceId} ao suporte.`
        : 'O servidor encontrou um erro inesperado. Tente novamente em instantes.'
    }
  }

  if (error instanceof TypeError && /fetch|network/i.test(error.message)) {
    return 'Não foi possível conectar à API. Verifique sua conexão e tente novamente.'
  }

  return 'Ocorreu um erro inesperado. Tente novamente.'
}

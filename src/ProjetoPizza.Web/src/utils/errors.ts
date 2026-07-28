import { ApiError } from '../api/httpClient'

const domainTranslations: Array<[RegExp, string]> = [
  [/category does not exist/i, 'A categoria informada não existe.'],
  [/linked table does not exist/i, 'A mesa vinculada não existe.'],
  [/table already belongs to an open session/i, 'A mesa já possui um atendimento aberto.'],
  [/a bill requires at least one valid order/i, 'A conta precisa ter ao menos um pedido válido.'],
  [/cash payments require an open cash shift/i, 'Abra o caixa antes de registrar pagamentos em dinheiro.'],
  [/split payment must contain between 2 and 50 people/i, 'Divida a conta entre 2 e 50 pessoas.'],
  [/split total must match the bill remaining amount/i, 'A soma das partes precisa corresponder ao saldo da conta. Recalcule a divisão.'],
  [/selected payment method is unavailable/i, 'Uma das formas de pagamento selecionadas não está disponível.'],
  [/received amount cannot be lower than payment amount/i, 'O valor recebido não pode ser menor que a parte da conta.'],
  [/payment method does not allow change/i, 'A forma de pagamento selecionada não permite troco.'],
  [/payment method requires an external reference/i, 'Informe a referência ou autorização do pagamento.'],
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

import { NumericFormat, type NumericFormatProps } from 'react-number-format'

type CurrencyInputProps = Omit<
  NumericFormatProps,
  'allowNegative' | 'decimalScale' | 'decimalSeparator' | 'fixedDecimalScale' | 'onValueChange' | 'prefix' | 'thousandSeparator' | 'type' | 'value'
> & {
  value: number
  onCurrencyValueChange: (value: number) => void
}

export function CurrencyInput({
  value,
  onCurrencyValueChange,
  onFocus,
  onMouseUp,
  ...props
}: CurrencyInputProps) {
  return (
    <NumericFormat
      {...props}
      value={Number.isFinite(value) ? value : 0}
      type="text"
      inputMode="decimal"
      prefix="R$ "
      thousandSeparator="."
      decimalSeparator=","
      decimalScale={2}
      fixedDecimalScale
      allowNegative={false}
      onValueChange={({ floatValue }) => onCurrencyValueChange(floatValue ?? 0)}
      onFocus={(event) => {
        onFocus?.(event)
        if (value === 0) event.currentTarget.select()
      }}
      onMouseUp={(event) => {
        onMouseUp?.(event)
        if (value === 0) {
          event.preventDefault()
          event.currentTarget.select()
        }
      }}
    />
  )
}

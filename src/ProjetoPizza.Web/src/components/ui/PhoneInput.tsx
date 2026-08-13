import { PatternFormat, type PatternFormatProps } from 'react-number-format'

type PhoneInputProps = Omit<
  PatternFormatProps,
  'format' | 'inputMode' | 'onValueChange' | 'type' | 'valueIsNumericString'
> & {
  onPhoneValueChange: (digits: string) => void
}

export function PhoneInput({ onPhoneValueChange, ...props }: PhoneInputProps) {
  return (
    <PatternFormat
      {...props}
      type="tel"
      inputMode="numeric"
      format="(##) #####-####"
      valueIsNumericString
      onValueChange={({ value }) => onPhoneValueChange(value)}
    />
  )
}

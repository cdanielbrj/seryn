import * as React from 'react'
import { Slot } from '@radix-ui/react-slot'
import { cva, type VariantProps } from 'class-variance-authority'
import { cn } from './classNames'

// shadcn/ui Button pattern, styled with Seryn's approved tokens.
const variants = cva('inline-flex min-h-11 items-center justify-center gap-2 rounded-xl px-4 py-2 text-sm font-semibold transition-colors disabled:pointer-events-none disabled:opacity-50', {
  variants: { variant: {
    default: 'seryn-primary-button bg-[var(--button-primary)] text-white hover:brightness-95',
    outline: 'border border-[var(--line)] bg-[var(--panel)] hover:bg-[var(--accent-tint)]',
    ghost: 'bg-transparent text-[var(--accent)] hover:bg-[var(--accent-tint)]',
  } }, defaultVariants: { variant: 'default' },
})

export function Button({ className, variant, asChild = false, ...props }: React.ComponentProps<'button'> & VariantProps<typeof variants> & { asChild?: boolean }) {
  const Component = asChild ? Slot : 'button'
  return <Component className={cn(variants({ variant, className }))} {...props} />
}

import { Component, input } from '@angular/core';

export type IconName = 'plus' | 'pencil' | 'trash';

/** Иконка-линия 16×16 цвета текста; смысл передаёт `aria-label` кнопки, сама иконка скрыта от экранных дикторов. */
@Component({
  selector: 'app-icon',
  templateUrl: './icon.html',
  styleUrl: './icon.css',
  host: {
    'aria-hidden': 'true',
  },
})
export class Icon {
  readonly name = input.required<IconName>();
}

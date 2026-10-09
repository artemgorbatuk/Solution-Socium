import { Component } from '@angular/core';
import { ThemeToggle } from '../theme-toggle/theme-toggle';

@Component({
  selector: 'app-topbar',
  imports: [ThemeToggle],
  template: '<app-theme-toggle />',
  styleUrl: './topbar.css',
  host: { role: 'banner' },
})
export class Topbar {}

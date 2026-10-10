import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Sidebar } from '../sidebar/sidebar';
import { Topbar } from '../topbar/topbar';

/** Раскладка приложения для вошедшего пользователя: боковая панель, верхняя панель и содержимое. */
@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, Sidebar, Topbar],
  styleUrl: './shell.css',
  templateUrl: './shell.html',
})
export class Shell {}

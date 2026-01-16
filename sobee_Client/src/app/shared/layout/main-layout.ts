import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Navbar } from '../components/navbar/navbar';
import { Footer } from '../components/footer/footer';

@Component({
  selector: 'app-main-layout',
  imports: [CommonModule, RouterModule, Navbar, Footer],
  template: `
    <div class="min-h-screen flex flex-col">
      <app-navbar></app-navbar>
      <main class="flex-1 pt-16">
        <ng-content></ng-content>
      </main>
      <app-footer></app-footer>
    </div>
  `,
  styles: []
})
export class MainLayout {}

import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MainLayout } from '../../shared/layout/main-layout';

@Component({
  selector: 'app-not-found',
  imports: [CommonModule, RouterModule, MainLayout],
  templateUrl: './not-found.html',
  styleUrl: './not-found.css'
})
export class NotFound {}

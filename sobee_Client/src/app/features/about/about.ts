import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MainLayout } from '../../shared/layout/main-layout';

@Component({
  selector: 'app-about',
  imports: [CommonModule, RouterModule, MainLayout],
  templateUrl: './about.html',
  styleUrl: './about.css'
})
export class About {
  teamMembers = [
    {
      name: 'Alex Rivera',
      role: 'Founder & CEO',
      image: 'https://placehold.co/200x200/f59e0b/white?text=AR',
      bio: 'Passionate about creating healthier energy alternatives.'
    },
    {
      name: 'Jordan Chen',
      role: 'Head of Product',
      image: 'https://placehold.co/200x200/f43f5e/white?text=JC',
      bio: 'Focused on perfecting our unique flavor profiles.'
    },
    {
      name: 'Sam Taylor',
      role: 'Operations Lead',
      image: 'https://placehold.co/200x200/8b5cf6/white?text=ST',
      bio: 'Ensuring every bottle meets our quality standards.'
    }
  ];

  values = [
    {
      title: 'Quality First',
      description: 'We never compromise on ingredients. Every component is carefully selected for purity and effectiveness.',
      icon: 'star'
    },
    {
      title: 'Transparency',
      description: 'What you see is what you get. We believe in honest labeling and clear communication.',
      icon: 'eye'
    },
    {
      title: 'Sustainability',
      description: 'From sourcing to packaging, we make choices that respect our planet.',
      icon: 'globe'
    },
    {
      title: 'Community',
      description: 'We\'re building more than a brand - we\'re building a community of mindful consumers.',
      icon: 'users'
    }
  ];
}

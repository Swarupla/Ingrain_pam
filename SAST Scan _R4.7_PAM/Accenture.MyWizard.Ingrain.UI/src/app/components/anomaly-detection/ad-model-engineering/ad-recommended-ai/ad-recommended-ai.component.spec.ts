import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdRecommendedAiComponent } from './ad-recommended-ai.component';

describe('AdRecommendedAiComponent', () => {
  let component: AdRecommendedAiComponent;
  let fixture: ComponentFixture<AdRecommendedAiComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ AdRecommendedAiComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(AdRecommendedAiComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

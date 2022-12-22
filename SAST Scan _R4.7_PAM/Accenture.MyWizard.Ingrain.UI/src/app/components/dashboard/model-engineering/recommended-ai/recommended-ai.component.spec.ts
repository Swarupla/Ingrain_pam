import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { RecommendedAIComponent } from './recommended-ai.component';

describe('RecommendedAIComponent', () => {
  let component: RecommendedAIComponent;
  let fixture: ComponentFixture<RecommendedAIComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ RecommendedAIComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(RecommendedAIComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

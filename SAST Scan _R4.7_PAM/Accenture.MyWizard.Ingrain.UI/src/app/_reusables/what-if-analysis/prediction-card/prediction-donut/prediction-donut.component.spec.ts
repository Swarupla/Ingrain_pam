import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { PredictionDonutComponent } from './prediction-donut.component';

describe('PredictionDonutComponent', () => {
  let component: PredictionDonutComponent;
  let fixture: ComponentFixture<PredictionDonutComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ PredictionDonutComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(PredictionDonutComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

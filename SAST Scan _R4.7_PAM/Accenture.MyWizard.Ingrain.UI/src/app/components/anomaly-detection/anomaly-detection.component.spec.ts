import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AnomalyDetectionComponent } from './anomaly-detection.component';

describe('AnomalyDetectionComponent', () => {
  let component: AnomalyDetectionComponent;
  let fixture: ComponentFixture<AnomalyDetectionComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ AnomalyDetectionComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(AnomalyDetectionComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

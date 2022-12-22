import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CustomQuerySaveComponent } from './custom-query-save.component';

describe('CustomQuerySaveComponent', () => {
  let component: CustomQuerySaveComponent;
  let fixture: ComponentFixture<CustomQuerySaveComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ CustomQuerySaveComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(CustomQuerySaveComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
